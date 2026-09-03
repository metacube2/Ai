using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;

namespace ZzprdatOrderTool
{
    internal static class Program
    {
        private const string ExpectedSystem = "T76";
        private const string ExpectedClient = "100";
        private const string Host = "travt762.sap.trafag.com";
        private const string SystemNumber = "00";
        private const string User = "KOI";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var originalOutput = Console.Out;
            using (var log = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zzprdat_order_tool.log"), false, Encoding.UTF8))
            using (var output = new DualWriter(originalOutput, log))
            {
                log.AutoFlush = true;
                Console.SetOut(output);
                try
                {
                    if (Environment.Is64BitProcess)
                        throw new InvalidOperationException("Dieses Werkzeug muss als x86-Prozess laufen.");

                    var options = Options.Parse(args);
                    var destination = CreateDestination();
                    destination.Ping();
                    AssertTestSystem(destination);

                    Console.WriteLine("Guard: SAP-System T76, Mandant 100 bestaetigt.");
                    switch (options.Action)
                    {
                        case "metadata":
                            PrintMetadata(destination);
                            break;
                        case "verify":
                            VerifyOrder(destination, options.OrderNumber);
                            break;
                        case "create":
                            CreateOrder(destination, options);
                            break;
                        case "release":
                            ReleaseOrder(destination, options);
                            break;
                        case "change-end":
                            ChangeEndDate(destination, options);
                            break;
                        default:
                            throw new InvalidOperationException("Unbekannte Aktion: " + options.Action);
                    }
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("ERROR: " + ex.GetType().Name + ": " + ex.Message);
                    return 1;
                }
            }
        }

        private static void PrintMetadata(RfcDestination destination)
        {
            PrintParameterMetadata(destination, "BAPI_PRODORD_CREATE", "ORDERDATA");
            PrintParameterMetadata(destination, "BAPI_PRODORD_RELEASE", "ORDERS");
            PrintParameterMetadata(destination, "BAPI_PRODORD_CHANGE", "ORDERDATA");
            PrintParameterMetadata(destination, "BAPI_PRODORD_CHANGE", "ORDERDATAX");
        }

        private static void CreateOrder(RfcDestination destination, Options options)
        {
            RequireConfirmation(options.Confirmed, "--confirm-create");
            Console.WriteLine("CREATE: Material 36385, Werk 1100, Auftragsart PP21, Menge 1 ST, Version 1001");
            Console.WriteLine("Termine: " + options.StartDate + " bis " + options.EndDate);

            RfcSessionManager.BeginContext(destination);
            try
            {
                var create = destination.Repository.CreateFunction("BAPI_PRODORD_CREATE");
                var orderData = create.GetStructure("ORDERDATA");
                orderData.SetValue("MATERIAL", "000000000000036385");
                orderData.SetValue("PLANT", "1100");
                orderData.SetValue("PLANNING_PLANT", "1100");
                orderData.SetValue("ORDER_TYPE", "PP21");
                orderData.SetValue("BASIC_START_DATE", options.StartDate);
                orderData.SetValue("BASIC_END_DATE", options.EndDate);
                orderData.SetValue("QUANTITY", 1m);
                orderData.SetValue("QUANTITY_UOM", "ST");
                orderData.SetValue("PROD_VERSION", "1001");
                orderData.SetValue("STORAGE_LOCATION", "0001");
                create.Invoke(destination);

                var returnValue = create.GetStructure("RETURN");
                PrintReturn("CREATE", returnValue);
                var orderNumber = create.GetString("ORDER_NUMBER").Trim();
                if (IsError(returnValue) || string.IsNullOrWhiteSpace(orderNumber))
                {
                    Rollback(destination);
                    throw new InvalidOperationException("Auftrag wurde nicht angelegt; Transaktion zurueckgerollt.");
                }

                Commit(destination);
                Console.WriteLine("Angelegt und committed: " + orderNumber);
                VerifyOrder(destination, orderNumber);
            }
            finally
            {
                RfcSessionManager.EndContext(destination);
            }
        }

        private static void ReleaseOrder(RfcDestination destination, Options options)
        {
            RequireConfirmation(options.Confirmed, "--confirm-release");
            RfcSessionManager.BeginContext(destination);
            try
            {
                var release = destination.Repository.CreateFunction("BAPI_PRODORD_RELEASE");
                var orders = release.GetTable("ORDERS");
                orders.Append();
                orders.SetValue("ORDER_NUMBER", NormalizeOrderNumber(options.OrderNumber));
                release.Invoke(destination);

                var hasErrors = false;
                hasErrors |= PrintReturnIfPresent(release, "RETURN", "RELEASE");
                hasErrors |= PrintReturnTableIfPresent(release, "DETAIL_RETURN", "RELEASE DETAIL");
                if (hasErrors)
                {
                    Rollback(destination);
                    throw new InvalidOperationException("Freigabe meldet Fehler; Transaktion zurueckgerollt.");
                }

                Commit(destination);
                Console.WriteLine("Freigabe committed: " + NormalizeOrderNumber(options.OrderNumber));
                VerifyOrder(destination, options.OrderNumber);
            }
            finally
            {
                RfcSessionManager.EndContext(destination);
            }
        }

        private static void ChangeEndDate(RfcDestination destination, Options options)
        {
            RequireConfirmation(options.Confirmed, "--confirm-change");
            RfcSessionManager.BeginContext(destination);
            try
            {
                var change = destination.Repository.CreateFunction("BAPI_PRODORD_CHANGE");
                change.SetValue("NUMBER", NormalizeOrderNumber(options.OrderNumber));
                change.GetStructure("ORDERDATA").SetValue("BASIC_END_DATE", options.EndDate);
                change.GetStructure("ORDERDATAX").SetValue("BASIC_END_DATE", "X");
                change.Invoke(destination);

                var returnValue = change.GetStructure("RETURN");
                PrintReturn("CHANGE", returnValue);
                if (IsError(returnValue))
                {
                    Rollback(destination);
                    throw new InvalidOperationException("Terminaenderung meldet Fehler; Transaktion zurueckgerollt.");
                }

                Commit(destination);
                Console.WriteLine("Terminaenderung committed: " + NormalizeOrderNumber(options.OrderNumber));
                VerifyOrder(destination, options.OrderNumber);
            }
            finally
            {
                RfcSessionManager.EndContext(destination);
            }
        }

        private static void Commit(RfcDestination destination)
        {
            var commit = destination.Repository.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.SetValue("WAIT", "X");
            commit.Invoke(destination);
            if (PrintReturnIfPresent(commit, "RETURN", "COMMIT"))
                throw new InvalidOperationException("BAPI commit meldet einen Fehler.");
        }

        private static void Rollback(RfcDestination destination)
        {
            var rollback = destination.Repository.CreateFunction("BAPI_TRANSACTION_ROLLBACK");
            rollback.Invoke(destination);
            Console.WriteLine("ROLLBACK ausgefuehrt.");
        }

        private static void VerifyOrder(RfcDestination destination, string orderNumber)
        {
            var normalized = NormalizeOrderNumber(orderNumber);
            Console.WriteLine();
            Console.WriteLine("Verifikation fuer Auftrag " + normalized);
            ReadTable(destination, "AUFK", new[] { "AUFNR", "AUART", "ERDAT", "ZZPRDAT", "OBJNR" }, "AUFNR = '" + normalized + "'");
            ReadTable(destination, "AFKO", new[] { "AUFNR", "GSTRP", "GLTRP", "FTRMI", "GETRI" }, "AUFNR = '" + normalized + "'");
        }

        private static void ReadTable(RfcDestination destination, string tableName, IEnumerable<string> fields, string where)
        {
            var read = destination.Repository.CreateFunction("RFC_READ_TABLE");
            read.SetValue("QUERY_TABLE", tableName);
            read.SetValue("DELIMITER", "|");
            read.SetValue("ROWCOUNT", 10);
            var fieldTable = read.GetTable("FIELDS");
            foreach (var field in fields)
            {
                fieldTable.Append();
                fieldTable.SetValue("FIELDNAME", field);
            }
            var options = read.GetTable("OPTIONS");
            options.Append();
            options.SetValue("TEXT", where);
            read.Invoke(destination);
            Console.WriteLine(tableName + ": " + string.Join(" | ", fields));
            var data = read.GetTable("DATA");
            foreach (IRfcStructure row in data)
                Console.WriteLine("  " + row.GetString("WA"));
            if (data.RowCount == 0)
                Console.WriteLine("  <keine Zeile>");
        }

        private static bool PrintReturnIfPresent(IRfcFunction function, string parameterName, string label)
        {
            try
            {
                var result = function.GetStructure(parameterName);
                PrintReturn(label, result);
                return IsError(result);
            }
            catch (RfcInvalidParameterException)
            {
                return false;
            }
            catch (RfcTypeConversionException)
            {
                return PrintReturnTableIfPresent(function, parameterName, label);
            }
        }

        private static bool PrintReturnTableIfPresent(IRfcFunction function, string parameterName, string label)
        {
            try
            {
                var result = function.GetTable(parameterName);
                var hasErrors = false;
                foreach (IRfcStructure row in result)
                {
                    PrintReturn(label, row);
                    hasErrors |= IsError(row);
                }
                return hasErrors;
            }
            catch (RfcInvalidParameterException)
            {
                return false;
            }
        }

        private static void PrintReturn(string label, IRfcStructure result)
        {
            var type = GetIfPresent(result, "TYPE");
            var id = GetIfPresent(result, "ID");
            var number = GetIfPresent(result, "NUMBER");
            var message = GetIfPresent(result, "MESSAGE");
            Console.WriteLine(label + ": TYPE=" + type + " ID=" + id + " NUMBER=" + number + " MESSAGE=" + message);
        }

        private static string GetIfPresent(IRfcStructure structure, string fieldName)
        {
            try { return structure.GetString(fieldName).Trim(); }
            catch (RfcInvalidParameterException) { return string.Empty; }
        }

        private static bool IsError(IRfcStructure result)
        {
            var type = GetIfPresent(result, "TYPE").ToUpperInvariant();
            return type == "E" || type == "A" || type == "X";
        }

        private static string NormalizeOrderNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Auftragsnummer fehlt.");
            var trimmed = value.Trim();
            if (trimmed.Any(ch => !char.IsDigit(ch)) || trimmed.Length > 12)
                throw new InvalidOperationException("Ungueltige Auftragsnummer: " + value);
            return trimmed.PadLeft(12, '0');
        }

        private static void RequireConfirmation(bool confirmed, string switchName)
        {
            if (!confirmed)
                throw new InvalidOperationException("Schreibaktion gesperrt. Erforderlich: " + switchName);
        }

        private static RfcDestination CreateDestination()
        {
            var password = Environment.GetEnvironmentVariable("SAP_NCO_PASSWORD");
            if (string.IsNullOrEmpty(password))
                password = ReadPassword("Passwort fuer KOI@T76/100: ");

            var parameters = new RfcConfigParameters
            {
                { RfcConfigParameters.Name, "ZZPRDAT_T76_" + Guid.NewGuid().ToString("N") },
                { RfcConfigParameters.AppServerHost, Host },
                { RfcConfigParameters.SystemNumber, SystemNumber },
                { RfcConfigParameters.Client, ExpectedClient },
                { RfcConfigParameters.User, User },
                { RfcConfigParameters.Password, password },
                { RfcConfigParameters.Language, "DE" },
                { RfcConfigParameters.PoolSize, "1" },
                { RfcConfigParameters.PeakConnectionsLimit, "1" }
            };
            return RfcDestinationManager.GetDestination(parameters);
        }

        private static void AssertTestSystem(RfcDestination destination)
        {
            var info = destination.Repository.CreateFunction("RFC_SYSTEM_INFO");
            info.Invoke(destination);
            var system = info.GetStructure("RFCSI_EXPORT").GetString("RFCSYSID").Trim();
            var configuredClient = destination.Parameters[RfcConfigParameters.Client];
            if (!string.Equals(system, ExpectedSystem, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(configuredClient, ExpectedClient, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Schreibschutz: erwartet T76/100, verbunden mit " + system + "/" + configuredClient + ".");
            }
        }

        private static void PrintParameterMetadata(RfcDestination destination, string functionName, string parameterName)
        {
            var function = destination.Repository.CreateFunction(functionName);
            IRfcStructure structure;
            try
            {
                structure = function.GetStructure(parameterName);
            }
            catch (RfcTypeConversionException)
            {
                var table = function.GetTable(parameterName);
                table.Append();
                structure = table.CurrentRow;
            }
            Console.WriteLine();
            Console.WriteLine(functionName + "." + parameterName + " (" + structure.Metadata.FieldCount + " Felder)");
            for (var index = 0; index < structure.Metadata.FieldCount; index++)
            {
                var field = structure.Metadata[index];
                Console.WriteLine(string.Format("  {0,-25} {1,-10} len={2}", field.Name, field.DataType, field.NucLength));
            }
        }

        private sealed class Options
        {
            public string Action { get; private set; } = "metadata";
            public string OrderNumber { get; private set; }
            public string StartDate { get; private set; }
            public string EndDate { get; private set; }
            public bool Confirmed { get; private set; }

            public static Options Parse(string[] args)
            {
                var result = new Options();
                var values = args ?? new string[0];
                if (values.Length == 0)
                    return result;

                result.Action = values[0].Trim().ToLowerInvariant();
                var today = DateTime.Today;
                result.StartDate = today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                result.EndDate = today.AddDays(14).ToString("yyyyMMdd", CultureInfo.InvariantCulture);

                for (var index = 1; index < values.Length; index++)
                {
                    switch (values[index].ToLowerInvariant())
                    {
                        case "--order":
                            result.OrderNumber = RequireValue(values, ref index, "--order");
                            break;
                        case "--start":
                            result.StartDate = ParseDate(RequireValue(values, ref index, "--start"));
                            break;
                        case "--end":
                            result.EndDate = ParseDate(RequireValue(values, ref index, "--end"));
                            break;
                        case "--confirm-create":
                            if (result.Action != "create") throw new InvalidOperationException("--confirm-create passt nicht zur Aktion.");
                            result.Confirmed = true;
                            break;
                        case "--confirm-release":
                            if (result.Action != "release") throw new InvalidOperationException("--confirm-release passt nicht zur Aktion.");
                            result.Confirmed = true;
                            break;
                        case "--confirm-change":
                            if (result.Action != "change-end") throw new InvalidOperationException("--confirm-change passt nicht zur Aktion.");
                            result.Confirmed = true;
                            break;
                        default:
                            throw new InvalidOperationException("Unbekanntes Argument: " + values[index]);
                    }
                }

                if ((result.Action == "verify" || result.Action == "release" || result.Action == "change-end") &&
                    string.IsNullOrWhiteSpace(result.OrderNumber))
                    throw new InvalidOperationException("Die Aktion erfordert --order <Nummer>.");

                return result;
            }

            private static string RequireValue(string[] values, ref int index, string name)
            {
                if (++index >= values.Length)
                    throw new InvalidOperationException("Wert fehlt nach " + name + ".");
                return values[index];
            }

            private static string ParseDate(string value)
            {
                DateTime parsed;
                var formats = new[] { "yyyyMMdd", "yyyy-MM-dd" };
                if (!DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    throw new InvalidOperationException("Datum muss yyyyMMdd oder yyyy-MM-dd sein: " + value);
                return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            }
        }

        private sealed class DualWriter : TextWriter
        {
            private readonly TextWriter _first;
            private readonly TextWriter _second;

            public DualWriter(TextWriter first, TextWriter second)
            {
                _first = first;
                _second = second;
            }

            public override Encoding Encoding { get { return _first.Encoding; } }
            public override void Write(char value) { _first.Write(value); _second.Write(value); }
            public override void Write(string value) { _first.Write(value); _second.Write(value); }
            public override void WriteLine(string value) { _first.WriteLine(value); _second.WriteLine(value); }
            protected override void Dispose(bool disposing)
            {
                if (disposing) _second.Flush();
                base.Dispose(disposing);
            }
        }

        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);
            var value = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return value.ToString();
                }
                if (key.Key == ConsoleKey.Escape)
                    throw new OperationCanceledException("Passworteingabe abgebrochen.");
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                    {
                        value.Length--;
                        Console.Write("\b \b");
                    }
                    continue;
                }
                if (!char.IsControl(key.KeyChar))
                {
                    value.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }
    }
}
