using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;

namespace SapProbe
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                var options = CliOptions.Parse(args);

                if (options.ShowHelp)
                {
                    PrintHelp();
                    return 0;
                }

                PrintBanner(options);

                if (Environment.Is64BitProcess)
                {
                    Console.Error.WriteLine("ERROR: This tool must run as x86 because SAP NCo is installed in the 32-bit GAC.");
                    return 2;
                }

                if (options.Command == SapCommand.LoadOnly)
                {
                    Console.WriteLine("Connector load : OK");
                    return 0;
                }

                var destination = CreateDestination(options);
                destination.Ping();

                if (!options.Quiet)
                {
                    Console.WriteLine("Target         : " + options.AppServerHost + " / SYSNR " + options.SystemNumber + " / CLIENT " + options.Client + " / USER " + options.User);
                    Console.WriteLine("Ping           : OK");
                    Console.WriteLine();
                }

                switch (options.Command)
                {
                    case SapCommand.SystemInfo:
                        RunSystemInfo(destination);
                        break;
                    case SapCommand.TableRead:
                        RunTableRead(destination, options);
                        break;
                    case SapCommand.TableFields:
                        RunTableFields(destination, options);
                        break;
                    case SapCommand.FieldExists:
                        RunFieldExists(destination, options);
                        break;
                    case SapCommand.FunctionInfo:
                        RunFunctionInfo(destination, options.FunctionName);
                        break;
                    case SapCommand.FunctionSearch:
                        RunFunctionSearch(destination, options.SearchPattern, options);
                        break;
                    case SapCommand.RfcCall:
                        RunRfcCall(destination, options);
                        break;
                    case SapCommand.AbapRead:
                        RunAbapRead(destination, options);
                        break;
                    case SapCommand.AbapCheck:
                        RunAbapCheck(destination, options);
                        break;
                    case SapCommand.AbapWrite:
                        RunAbapWrite(destination, options);
                        break;
                    case SapCommand.AbapActivate:
                        RunAbapActivate(destination, options);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Command", "Unsupported command: " + options.Command);
                }

                return 0;
            }
            catch (RfcLogonException ex)
            {
                return Fail("SAP logon failed", ex);
            }
            catch (RfcCommunicationException ex)
            {
                return Fail("SAP communication failed", ex);
            }
            catch (RfcAbapRuntimeException ex)
            {
                return Fail("ABAP runtime error", ex);
            }
            catch (RfcAbapException ex)
            {
                return Fail("ABAP exception", ex);
            }
            catch (Exception ex)
            {
                return Fail("SAP CLI failed", ex);
            }
        }

        private static RfcDestination CreateDestination(CliOptions options)
        {
            var password = Environment.GetEnvironmentVariable("SAP_NCO_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                password = Environment.GetEnvironmentVariable("SAP_T76_PASSWORD");
            }

            if (string.IsNullOrEmpty(password))
            {
                if (options.NoPasswordPrompt)
                {
                    throw new InvalidOperationException("No SAP password was provided. Set SAP_NCO_PASSWORD/SAP_T76_PASSWORD or allow the masked password prompt.");
                }

                password = ReadPassword("Password for " + options.User + "@" + options.Client + ": ");
            }

            var parameters = new RfcConfigParameters
            {
                { RfcConfigParameters.Name, options.Name },
                { RfcConfigParameters.AppServerHost, options.AppServerHost },
                { RfcConfigParameters.SystemNumber, options.SystemNumber },
                { RfcConfigParameters.Client, options.Client },
                { RfcConfigParameters.User, options.User },
                { RfcConfigParameters.Password, password },
                { RfcConfigParameters.Language, options.Language },
                { RfcConfigParameters.PoolSize, "1" },
                { RfcConfigParameters.PeakConnectionsLimit, "1" },
                { RfcConfigParameters.ConnectionIdleTimeout, "600" }
            };

            if (!string.IsNullOrWhiteSpace(options.SapRouter))
            {
                parameters.Add(RfcConfigParameters.SAPRouter, options.SapRouter);
            }

            if (!string.IsNullOrWhiteSpace(options.Trace))
            {
                parameters.Add(RfcConfigParameters.Trace, options.Trace);
            }

            return RfcDestinationManager.GetDestination(parameters);
        }

        private static void RunSystemInfo(RfcDestination destination)
        {
            var function = destination.Repository.CreateFunction("RFC_SYSTEM_INFO");
            function.Invoke(destination);

            var info = function.GetStructure("RFCSI_EXPORT");
            Console.WriteLine("RFC_SYSTEM_INFO");
            PrintField(info, "RFCSYSID", "SAP System ID");
            PrintField(info, "RFCHOST", "RFC Host");
            PrintField(info, "RFCHOST2", "Host 2");
            PrintField(info, "RFCIPADDR", "IP Address");
            PrintField(info, "RFCOPSYS", "OS");
            PrintField(info, "RFCMACH", "Machine");
            PrintField(info, "RFCDBSYS", "DB System");
            PrintField(info, "RFCDBHOST", "DB Host");
            PrintField(info, "RFCDATABS", "DB Name");
            PrintField(info, "RFCKERNRL", "Kernel Release");
        }

        private static void RunTableRead(RfcDestination destination, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.TableName))
            {
                throw new ArgumentException("Missing table name. Example: SapProbe.exe table-read T000 --fields MANDT,MTEXT --rowcount 5");
            }

            var function = destination.Repository.CreateFunction("RFC_READ_TABLE");
            function.SetValue("QUERY_TABLE", options.TableName.ToUpperInvariant());
            function.SetValue("DELIMITER", options.Delimiter);
            function.SetValue("ROWSKIPS", options.RowSkip);
            function.SetValue("ROWCOUNT", options.RowCount);

            var fields = function.GetTable("FIELDS");
            foreach (var field in options.Fields)
            {
                fields.Append();
                fields.SetValue("FIELDNAME", field.ToUpperInvariant());
            }

            var whereOptions = function.GetTable("OPTIONS");
            foreach (var where in options.WhereClauses)
            {
                foreach (var line in SplitAbapOptionLine(where, 72))
                {
                    whereOptions.Append();
                    whereOptions.SetValue("TEXT", line);
                }
            }

            function.Invoke(destination);

            var resolvedFields = ReadReadTableFieldNames(fields);
            var data = function.GetTable("DATA");
            var rows = new List<List<string>>();

            for (var i = 0; i < data.RowCount; i++)
            {
                data.CurrentIndex = i;
                rows.Add(SplitDataRow(data.GetString("WA"), options.Delimiter, resolvedFields.Count));
            }

            RenderRows(resolvedFields, rows, options.OutputFormat);
        }

        private static void RunTableFields(RfcDestination destination, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.TableName))
            {
                throw new ArgumentException("Missing table name. Example: SapProbe.exe table-fields MARC");
            }

            var fields = GetTableFieldRows(destination, options.TableName, options.FieldName);
            var headers = new[] { "FIELDNAME", "KEYFLAG", "DATATYPE", "LENG", "DECIMALS", "ROLLNAME", "FIELDTEXT" };

            var visibleFields = fields.Take(options.MaxTableRows).ToList();
            RenderRows(headers, visibleFields, options.OutputFormat);
            if (fields.Count > visibleFields.Count)
            {
                Console.WriteLine("... " + (fields.Count - visibleFields.Count) + " more fields not printed. Increase --max-table-rows if needed.");
            }
        }

        private static void RunFieldExists(RfcDestination destination, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.TableName) || string.IsNullOrWhiteSpace(options.FieldName))
            {
                throw new ArgumentException("Missing table or field. Example: SapProbe.exe field-exists MARC MMSTA");
            }

            var fields = GetTableFieldRows(destination, options.TableName, options.FieldName);
            Console.WriteLine("Table          : " + options.TableName.ToUpperInvariant());
            Console.WriteLine("Field          : " + options.FieldName.ToUpperInvariant());
            Console.WriteLine("Exists         : " + (fields.Count > 0 ? "YES" : "NO"));

            if (fields.Count > 0)
            {
                Console.WriteLine();
                RenderRows(new[] { "FIELDNAME", "KEYFLAG", "DATATYPE", "LENG", "DECIMALS", "ROLLNAME", "FIELDTEXT" }, fields, options.OutputFormat);
            }
        }

        private static List<List<string>> GetTableFieldRows(RfcDestination destination, string tableName, string fieldName)
        {
            var function = destination.Repository.CreateFunction("DDIF_FIELDINFO_GET");
            function.SetValue("TABNAME", tableName.ToUpperInvariant());
            function.SetValue("LANGU", "D");
            function.SetValue("DO_NOT_WRITE", "X");

            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                function.SetValue("FIELDNAME", fieldName.ToUpperInvariant());
            }

            function.Invoke(destination);

            var table = function.GetTable("DFIES_TAB");
            var rows = new List<List<string>>();
            for (var i = 0; i < table.RowCount; i++)
            {
                table.CurrentIndex = i;
                rows.Add(new List<string>
                {
                    SafeGetString(table.CurrentRow, "FIELDNAME"),
                    SafeGetString(table.CurrentRow, "KEYFLAG"),
                    SafeGetString(table.CurrentRow, "DATATYPE"),
                    SafeGetString(table.CurrentRow, "LENG"),
                    SafeGetString(table.CurrentRow, "DECIMALS"),
                    SafeGetString(table.CurrentRow, "ROLLNAME"),
                    SafeGetString(table.CurrentRow, "FIELDTEXT")
                });
            }

            return rows;
        }

        private static void RunFunctionInfo(RfcDestination destination, string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException("Missing function name. Example: SapProbe.exe function-info RFC_SYSTEM_INFO");
            }

            var metadata = destination.Repository.GetFunctionMetadata(functionName.ToUpperInvariant());
            Console.WriteLine("Function: " + functionName.ToUpperInvariant());
            Console.WriteLine();
            Console.WriteLine("Direction  Name                              Type       Length  Decimals  Default");
            Console.WriteLine("---------  --------------------------------  ---------  ------  --------  -------");

            for (var i = 0; i < metadata.ParameterCount; i++)
            {
                var parameter = metadata[i];
                Console.WriteLine(
                    parameter.Direction.ToString().PadRight(9) + "  " +
                    parameter.Name.PadRight(32) + "  " +
                    parameter.DataType.ToString().PadRight(9) + "  " +
                    parameter.NucLength.ToString().PadLeft(6) + "  " +
                    parameter.Decimals.ToString().PadLeft(8) + "  " +
                    (parameter.DefaultValue ?? string.Empty));
            }
        }

        private static void RunFunctionSearch(RfcDestination destination, string pattern, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException("Missing search pattern. Example: SapProbe.exe function-search RPY*PROGRAM*");
            }

            var function = destination.Repository.CreateFunction("RFC_FUNCTION_SEARCH");
            function.SetValue("FUNCNAME", pattern.ToUpperInvariant());
            function.Invoke(destination);

            Console.WriteLine("Function search: " + pattern.ToUpperInvariant());
            Console.WriteLine();
            DumpTable(function.GetTable("FUNCTIONS"), options.MaxTableRows, options.OutputFormat);
        }

        private static void RunRfcCall(RfcDestination destination, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.FunctionName))
            {
                throw new ArgumentException("Missing function name. Example: SapProbe.exe rfc-call STFC_CONNECTION --set REQUTEXT=hello");
            }

            var functionName = options.FunctionName.ToUpperInvariant();
            var metadata = destination.Repository.GetFunctionMetadata(functionName);
            var function = metadata.CreateFunction();

            foreach (var pair in options.SetValues)
            {
                function.SetValue(pair.Key.ToUpperInvariant(), pair.Value);
            }

            function.Invoke(destination);

            Console.WriteLine("Function: " + functionName);
            Console.WriteLine();
            DumpFunctionResult(function, metadata, options);
        }

        private static void RunAbapRead(RfcDestination destination, CliOptions options)
        {
            var programName = RequireProgramName(options);
            var lines = ReadAbapProgram(destination, programName, options);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                var fullPath = Path.GetFullPath(options.OutputPath);
                File.WriteAllLines(fullPath, lines, Encoding.UTF8);
                Console.WriteLine("Program        : " + programName);
                Console.WriteLine("Lines          : " + lines.Count);
                Console.WriteLine("Output         : " + fullPath);
                return;
            }

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }

        private static void RunAbapCheck(RfcDestination destination, CliOptions options)
        {
            var programName = RequireProgramName(options);
            AbapSyntaxResult result;
            var lineCount = 0;

            if (string.IsNullOrWhiteSpace(options.SourceFile))
            {
                result = CheckExistingAbapProgram(destination, programName, options);
            }
            else
            {
                var lines = LoadSourceForAbapCommand(destination, programName, options);
                lineCount = lines.Count;
                result = CheckAbapSyntax(destination, programName, lines);
            }

            Console.WriteLine("Program        : " + programName);
            if (lineCount > 0)
            {
                Console.WriteLine("Lines          : " + lineCount);
            }
            Console.WriteLine("Syntax status  : " + (result.ErrorSubrc == 0 ? "OK" : "ERROR"));
            Console.WriteLine("Error subrc    : " + result.ErrorSubrc);

            if (result.ErrorSubrc != 0)
            {
                Console.WriteLine("Error include  : " + result.ErrorInclude);
                Console.WriteLine("Error line     : " + result.ErrorLine);
                Console.WriteLine("Error offset   : " + result.ErrorOffset);
                Console.WriteLine("Error word     : " + result.ErrorWord);
                Console.WriteLine("Error message  : " + result.ErrorMessage);
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine("  " + warning);
                }
            }
        }

        private static void RunAbapWrite(RfcDestination destination, CliOptions options)
        {
            var programName = RequireProgramName(options);
            if (string.IsNullOrWhiteSpace(options.SourceFile))
            {
                throw new ArgumentException("abap-write requires --source-file <path>.");
            }

            var lines = LoadSourceForAbapCommand(destination, programName, options);
            Console.WriteLine("Program        : " + programName);
            Console.WriteLine("Source file    : " + Path.GetFullPath(options.SourceFile));
            Console.WriteLine("Lines          : " + lines.Count);
            Console.WriteLine("Pre-check      : skipped for local source; repository syntax can be checked after write.");

            if (options.DryRun)
            {
                Console.WriteLine("Dry run        : no SAP repository changes were written.");
                return;
            }

            if (!options.ConfirmWrite)
            {
                throw new InvalidOperationException("abap-write is blocked unless --confirm-write is provided.");
            }

            WriteAbapProgram(destination, options, programName, lines);

            Console.WriteLine("Lines written  : " + lines.Count);
            Console.WriteLine("Result         : RPY_PROGRAM_INSERT returned without RFC exception.");

            var syntax = CheckExistingAbapProgram(destination, programName, options);
            Console.WriteLine("Post-check     : " + (syntax.ErrorSubrc == 0 ? "OK" : "ERROR"));
            if (syntax.ErrorSubrc != 0)
            {
                Console.WriteLine("Error line     : " + syntax.ErrorLine);
                Console.WriteLine("Error message  : " + syntax.ErrorMessage);
            }
        }

        private static void RunAbapActivate(RfcDestination destination, CliOptions options)
        {
            var programName = RequireProgramName(options);
            var lines = LoadSourceForAbapCommand(destination, programName, options);

            Console.WriteLine("Program        : " + programName);
            Console.WriteLine("Lines          : " + lines.Count);
            Console.WriteLine("Activation     : RPY_PROGRAM_INSERT with SAVE_INACTIVE blank");

            if (options.DryRun)
            {
                Console.WriteLine("Dry run        : no SAP repository changes were written.");
                return;
            }

            if (!options.ConfirmWrite)
            {
                throw new InvalidOperationException("abap-activate is blocked unless --confirm-write is provided.");
            }

            var activateOptions = options.CloneForActiveWrite();
            WriteAbapProgram(destination, activateOptions, programName, lines);
            Console.WriteLine("Result         : activation write returned without RFC exception.");

            var syntax = CheckExistingAbapProgram(destination, programName, options);
            Console.WriteLine("Post-check     : " + (syntax.ErrorSubrc == 0 ? "OK" : "ERROR"));
            if (syntax.ErrorSubrc != 0)
            {
                Console.WriteLine("Error line     : " + syntax.ErrorLine);
                Console.WriteLine("Error message  : " + syntax.ErrorMessage);
            }
        }

        private static void WriteAbapProgram(RfcDestination destination, CliOptions options, string programName, IList<string> lines)
        {
            var function = destination.Repository.CreateFunction("RPY_PROGRAM_INSERT");
            function.SetValue("PROGRAM_NAME", programName);
            function.SetValue("TITLE_STRING", string.IsNullOrWhiteSpace(options.Title) ? programName : options.Title);
            function.SetValue("SUPPRESS_DIALOG", "X");
            function.SetValue("UCCHECK", "X");

            if (!string.IsNullOrWhiteSpace(options.DevelopmentClass))
            {
                function.SetValue("DEVELOPMENT_CLASS", options.DevelopmentClass);
            }

            if (!string.IsNullOrWhiteSpace(options.TransportNumber))
            {
                function.SetValue("TRANSPORT_NUMBER", options.TransportNumber);
            }

            if (options.SaveInactive)
            {
                function.SetValue("SAVE_INACTIVE", "X");
            }

            if (options.Temporary)
            {
                function.SetValue("TEMPORARY", "X");
            }

            FillSingleColumnTable(function.GetTable("SOURCE_EXTENDED"), lines);
            function.Invoke(destination);
        }


        private static void DumpFunctionResult(IRfcFunction function, RfcFunctionMetadata metadata, CliOptions options)
        {
            for (var i = 0; i < metadata.ParameterCount; i++)
            {
                var parameter = metadata[i];
                if (parameter.Direction == RfcDirection.IMPORT && !options.DumpImports)
                {
                    continue;
                }

                Console.WriteLine(parameter.Direction + " " + parameter.Name + " (" + parameter.DataType + ")");

                if (parameter.DataType == RfcDataType.TABLE)
                {
                    DumpTable(function.GetTable(parameter.Name), options.MaxTableRows, options.OutputFormat);
                }
                else if (parameter.DataType == RfcDataType.STRUCTURE)
                {
                    DumpStructure(function.GetStructure(parameter.Name));
                }
                else
                {
                    Console.WriteLine("  " + SafeGetString(function, parameter.Name));
                }

                Console.WriteLine();
            }
        }

        private static string RequireProgramName(CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ProgramName))
            {
                throw new ArgumentException("Missing ABAP program name.");
            }

            return options.ProgramName.ToUpperInvariant();
        }

        private static List<string> LoadSourceForAbapCommand(RfcDestination destination, string programName, CliOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.SourceFile))
            {
                var fullPath = Path.GetFullPath(options.SourceFile);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("ABAP source file was not found.", fullPath);
                }

                return File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
            }

            return ReadAbapProgram(destination, programName, options);
        }

        private static List<string> ReadAbapProgram(RfcDestination destination, string programName, CliOptions options)
        {
            var function = destination.Repository.CreateFunction("RPY_PROGRAM_READ");
            function.SetValue("PROGRAM_NAME", programName);
            function.SetValue("LANGUAGE", options.Language);
            function.SetValue("ONLY_SOURCE", "X");
            function.SetValue("ONLY_TEXTS", " ");
            function.SetValue("WITH_INCLUDELIST", options.WithIncludeList ? "X" : " ");
            function.SetValue("WITH_LOWERCASE", "X");
            function.SetValue("READ_LATEST_VERSION", options.ReadLatestVersion ? "X" : " ");
            function.Invoke(destination);

            var extended = ReadSingleColumnTable(function.GetTable("SOURCE_EXTENDED"));
            if (extended.Count > 0)
            {
                return extended;
            }

            return ReadSingleColumnTable(function.GetTable("SOURCE"));
        }

        private static AbapSyntaxResult CheckExistingAbapProgram(RfcDestination destination, string programName, CliOptions options)
        {
            var function = destination.Repository.CreateFunction("RS_ABAP_SYNTAX_CHECK_E");
            function.SetValue("P_PROGRAM", programName);
            function.SetValue("P_LANGU", options.Language);
            function.SetValue("P_NO_PACKAGE_CHECK", "X");
            function.Invoke(destination);

            var result = new AbapSyntaxResult
            {
                ErrorSubrc = SafeGetInt(function, "P_SUBRC"),
                ErrorInclude = string.Empty,
                ErrorWord = string.Empty,
                ErrorMessage = string.Empty
            };

            var errors = function.GetTable("P_ERRORS");
            for (var i = 0; i < errors.RowCount; i++)
            {
                errors.CurrentIndex = i;
                var kind = SafeGetString(errors.CurrentRow, "KIND");
                var message = SafeGetString(errors.CurrentRow, "MESSAGE");
                var line = SafeGetInt(errors.CurrentRow, "LINE");

                if (result.ErrorSubrc != 0 && string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    result.ErrorLine = line;
                    result.ErrorMessage = message;
                    result.ErrorInclude = SafeGetString(errors.CurrentRow, "INCNAME");
                    result.ErrorWord = SafeGetString(errors.CurrentRow, "KEYWORD");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    result.Warnings.Add((string.IsNullOrWhiteSpace(kind) ? "INFO" : kind) + " line " + line + ": " + message);
                }
            }

            var warnings = function.GetTable("P_WARNINGS");
            for (var i = 0; i < warnings.RowCount; i++)
            {
                warnings.CurrentIndex = i;
                var message = SafeGetString(warnings.CurrentRow, "MESSAGE");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    result.Warnings.Add("WARN line " + SafeGetInt(warnings.CurrentRow, "LINE") + ": " + message);
                }
            }

            return result;
        }

        private static AbapSyntaxResult CheckAbapSyntax(RfcDestination destination, string programName, IList<string> lines)
        {
            var function = destination.Repository.CreateFunction("RFC_PROGRAM_CHECK_SYNTAX");
            function.SetValue("SOURCE_NAME", programName);
            function.SetValue("GLOBAL_PROGRAM", programName);
            function.SetValue("REPLACING", "X");
            FillSingleColumnTable(function.GetTable("SOURCE"), lines);
            FillSingleColumnTable(function.GetTable("REPLACING_SOURCE"), lines);
            function.Invoke(destination);

            var result = new AbapSyntaxResult
            {
                ErrorSubrc = SafeGetInt(function, "ERROR_SUBRC"),
                ErrorInclude = SafeGetString(function, "ERROR_INCLUDE"),
                ErrorLine = SafeGetInt(function, "ERROR_LINE"),
                ErrorOffset = SafeGetInt(function, "ERROR_OFFSET"),
                ErrorWord = SafeGetString(function, "ERROR_WORD"),
                ErrorMessage = SafeGetString(function, "ERROR_MESSAGE")
            };

            result.Warnings.AddRange(ReadSingleColumnTable(function.GetTable("WARNINGS_TABLE")));
            if (result.ErrorSubrc != 0 && string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                var fallback = CheckAbapSyntaxWithErrorSyntaxCheck(destination, programName, lines);
                if (fallback != null)
                {
                    return fallback;
                }
            }

            return result;
        }

        private static AbapSyntaxResult CheckAbapSyntaxWithErrorSyntaxCheck(RfcDestination destination, string programName, IList<string> lines)
        {
            try
            {
                var function = destination.Repository.CreateFunction("RS_ABAP_ERROR_SYNTAX_CHECK");
                function.SetValue("P_PROGRAM", programName);
                function.SetValue("P_MODE", 0);
                FillSingleColumnTable(function.GetTable("P_REPTAB"), lines);
                function.Invoke(destination);

                var kind = SafeGetString(function, "P_KIND");
                var message = SafeGetString(function, "P_MESSAGE");
                var result = new AbapSyntaxResult
                {
                    ErrorSubrc = string.Equals(kind, "E", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(message) ? 4 : 0,
                    ErrorMessage = message,
                    ErrorInclude = string.Empty,
                    ErrorWord = string.Empty
                };

                result.Warnings.AddRange(ReadSingleColumnTable(function.GetTable("P_TRCTAB")));
                var error = function.GetStructure("P_ERROR");
                result.ErrorLine = SafeGetFirstInt(error, "LINE", "LINENO", "ROW");
                result.ErrorOffset = SafeGetFirstInt(error, "OFFSET", "COL", "COLUMN");
                result.ErrorWord = SafeGetFirstString(error, "WORD", "TOKEN");

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<string> ReadSingleColumnTable(IRfcTable table)
        {
            var lines = new List<string>();
            var columnIndex = PickTextColumnIndex(table.Metadata.LineType);

            for (var i = 0; i < table.RowCount; i++)
            {
                table.CurrentIndex = i;
                lines.Add(table.CurrentRow.GetString(columnIndex).TrimEnd());
            }

            return lines;
        }

        private static void FillSingleColumnTable(IRfcTable table, IEnumerable<string> lines)
        {
            var columnIndex = PickTextColumnIndex(table.Metadata.LineType);

            foreach (var line in lines)
            {
                table.Append();
                table.CurrentRow.SetValue(columnIndex, line ?? string.Empty);
            }
        }

        private static int PickTextColumnIndex(RfcStructureMetadata metadata)
        {
            var preferred = new[] { "LINE", "TEXT", "SOURCE", "WA" };
            foreach (var name in preferred)
            {
                var index = metadata.TryNameToIndex(name);
                if (index >= 0)
                {
                    return index;
                }
            }

            return 0;
        }

        private static void DumpTable(IRfcTable table, int maxRows, OutputFormat format)
        {
            var headers = GetContainerFieldNames(table.Metadata.LineType);
            var rows = new List<List<string>>();
            var rowLimit = Math.Min(table.RowCount, Math.Max(0, maxRows));

            for (var i = 0; i < rowLimit; i++)
            {
                table.CurrentIndex = i;
                rows.Add(ReadStructureValues(table.CurrentRow, headers));
            }

            RenderRows(headers, rows, format);

            if (table.RowCount > rowLimit)
            {
                Console.WriteLine("... " + (table.RowCount - rowLimit) + " more rows not printed. Increase --max-table-rows if needed.");
            }
        }

        private static void DumpStructure(IRfcStructure structure)
        {
            var headers = GetContainerFieldNames(structure.Metadata);
            foreach (var header in headers)
            {
                Console.WriteLine("  " + header.PadRight(32) + ": " + SafeGetString(structure, header));
            }
        }

        private static List<string> GetContainerFieldNames(RfcStructureMetadata metadata)
        {
            var names = new List<string>();
            for (var i = 0; i < metadata.FieldCount; i++)
            {
                names.Add(metadata[i].Name);
            }

            return names;
        }

        private static List<string> ReadStructureValues(IRfcStructure structure, IList<string> headers)
        {
            var values = new List<string>();
            foreach (var header in headers)
            {
                values.Add(SafeGetString(structure, header));
            }

            return values;
        }

        private static List<string> ReadReadTableFieldNames(IRfcTable fields)
        {
            var names = new List<string>();
            for (var i = 0; i < fields.RowCount; i++)
            {
                fields.CurrentIndex = i;
                var name = SafeGetString(fields.CurrentRow, "FIELDNAME").Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private static List<string> SplitDataRow(string row, string delimiter, int expectedColumns)
        {
            var parts = row.Split(new[] { delimiter }, StringSplitOptions.None)
                .Select(part => part.TrimEnd())
                .ToList();

            while (parts.Count < expectedColumns)
            {
                parts.Add(string.Empty);
            }

            if (expectedColumns > 0 && parts.Count > expectedColumns)
            {
                parts = parts.Take(expectedColumns).ToList();
            }

            return parts;
        }

        private static IEnumerable<string> SplitAbapOptionLine(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            var remaining = text.Trim();
            while (remaining.Length > maxLength)
            {
                var splitAt = remaining.LastIndexOf(' ', maxLength - 1, maxLength);
                if (splitAt < 1)
                {
                    splitAt = maxLength;
                }

                yield return remaining.Substring(0, splitAt).TrimEnd();
                remaining = remaining.Substring(splitAt).TrimStart();
            }

            if (remaining.Length > 0)
            {
                yield return remaining;
            }
        }

        private static void RenderRows(IList<string> headers, IList<List<string>> rows, OutputFormat format)
        {
            if (format == OutputFormat.Json)
            {
                RenderJson(headers, rows);
                return;
            }

            if (format == OutputFormat.Csv)
            {
                Console.WriteLine(string.Join(",", headers.Select(EscapeCsv)));
                foreach (var row in rows)
                {
                    Console.WriteLine(string.Join(",", row.Select(EscapeCsv)));
                }

                return;
            }

            Console.WriteLine(string.Join(" | ", headers));
            Console.WriteLine(string.Join("-+-", headers.Select(header => new string('-', Math.Max(3, header.Length)))));

            foreach (var row in rows)
            {
                Console.WriteLine(string.Join(" | ", row));
            }

            if (rows.Count == 0)
            {
                Console.WriteLine("(no rows)");
            }
        }

        private static void RenderJson(IList<string> headers, IList<List<string>> rows)
        {
            Console.WriteLine("[");
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                Console.Write("  {");

                for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                    {
                        Console.Write(", ");
                    }

                    var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    Console.Write("\"" + EscapeJson(headers[columnIndex]) + "\": \"" + EscapeJson(value) + "\"");
                }

                Console.Write(rowIndex + 1 == rows.Count ? "}" : "},");
                Console.WriteLine();
            }
            Console.WriteLine("]");
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(ch))
                        {
                            builder.Append("\\u" + ((int)ch).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(ch);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private static void PrintBanner(CliOptions options)
        {
            if (options.Quiet)
            {
                return;
            }

            Console.WriteLine("SAP NCo CLI");
            Console.WriteLine("Architecture   : " + (Environment.Is64BitProcess ? "x64" : "x86"));
            Console.WriteLine("NCo Assembly   : " + typeof(RfcDestinationManager).Assembly.FullName);
        }

        private static int Fail(string title, Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("ERROR: " + title);
            Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message);
            return 1;
        }

        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);
            var password = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return password.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Length--;
                        Console.Write("\b \b");
                    }

                    continue;
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    throw new OperationCanceledException("Password entry cancelled.");
                }

                if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }

        private static void PrintField(IRfcStructure structure, string fieldName, string label)
        {
            Console.WriteLine(label.PadRight(16) + ": " + SafeGetString(structure, fieldName));
        }

        private static string SafeGetString(IRfcDataContainer container, string fieldName)
        {
            try
            {
                return container.GetString(fieldName).Trim();
            }
            catch (Exception)
            {
                return "<not available>";
            }
        }

        private static int SafeGetInt(IRfcDataContainer container, string fieldName)
        {
            try
            {
                return container.GetInt(fieldName);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string SafeGetFirstString(IRfcDataContainer container, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                try
                {
                    var value = container.GetString(fieldName).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
                catch (Exception)
                {
                    // Try the next known field name.
                }
            }

            return string.Empty;
        }

        private static int SafeGetFirstInt(IRfcDataContainer container, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                try
                {
                    var value = container.GetInt(fieldName);
                    if (value != 0)
                    {
                        return value;
                    }
                }
                catch (Exception)
                {
                    // Try the next known field name.
                }
            }

            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SAP NCo CLI for T76");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  SapProbe.exe [global options] <command> [command options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  system-info                         Ping SAP and call RFC_SYSTEM_INFO. Default command.");
            Console.WriteLine("  table-read <table>                  Read table data via RFC_READ_TABLE.");
            Console.WriteLine("  table-fields <table> [field]        Show DDIC field metadata via DDIF_FIELDINFO_GET.");
            Console.WriteLine("  field-exists <table> <field>        Check whether a DDIC table field exists.");
            Console.WriteLine("  function-info <function>            Show RFC function interface metadata.");
            Console.WriteLine("  function-search <pattern>           Search RFC functions via RFC_FUNCTION_SEARCH.");
            Console.WriteLine("  rfc-call <function>                 Call an RFC-enabled function module.");
            Console.WriteLine("  abap-read <program>                 Read ABAP report/source via RPY_PROGRAM_READ.");
            Console.WriteLine("  abap-check <program>                Syntax-check an existing repository program.");
            Console.WriteLine("  abap-write <program>                Write ABAP source via RPY_PROGRAM_INSERT; requires --confirm-write.");
            Console.WriteLine("  abap-activate <program>             Activation attempt through active RPY_PROGRAM_INSERT write.");
            Console.WriteLine("  load-only                           Only load the 32-bit NCo assembly; do not connect.");
            Console.WriteLine();
            Console.WriteLine("Global options:");
            Console.WriteLine("  --ashost <host>                     ABAP application server. Default: travt762.sap.trafag.com");
            Console.WriteLine("  --sysnr <nr>                        SAP system number. Default: 00");
            Console.WriteLine("  --client <mandt>                    SAP client. Default: 100");
            Console.WriteLine("  --user <user>                       SAP user. Default: KOI");
            Console.WriteLine("  --lang <lang>                       SAP logon language. Default: DE");
            Console.WriteLine("  --router <route>                    Optional SAProuter string.");
            Console.WriteLine("  --trace <level>                     Optional NCo trace level.");
            Console.WriteLine("  --quiet                             Suppress connection banner.");
            Console.WriteLine("  --no-password-prompt                Fail if no password env var is set.");
            Console.WriteLine("  --help                              Show this help.");
            Console.WriteLine();
            Console.WriteLine("table-read options:");
            Console.WriteLine("  --fields A,B,C                      Comma-separated fields. Recommended to avoid RFC_READ_TABLE row limits.");
            Console.WriteLine("  --field FIELD                       Single field filter for table-fields.");
            Console.WriteLine("  --where \"FIELD = 'VALUE'\"            WHERE fragment; can be repeated.");
            Console.WriteLine("  --rowcount <n>                      Max rows. Default: 10.");
            Console.WriteLine("  --rowskip <n>                       Rows to skip. Default: 0.");
            Console.WriteLine("  --format table|csv|json             Output format. Default: table.");
            Console.WriteLine();
            Console.WriteLine("rfc-call options:");
            Console.WriteLine("  --set NAME=VALUE                    Scalar import/changing value; can be repeated.");
            Console.WriteLine("  --dump-imports                      Include import parameters in output.");
            Console.WriteLine("  --max-table-rows <n>                Max rows printed for table parameters. Default: 20.");
            Console.WriteLine("  --format table|csv|json             Format for table outputs. Default: table.");
            Console.WriteLine();
            Console.WriteLine("ABAP source options:");
            Console.WriteLine("  --out <path>                        Write abap-read output to a local file.");
            Console.WriteLine("  --source-file <path>                Use a local source file for abap-check/abap-write.");
            Console.WriteLine("  --latest                            Read latest version in abap-read/abap-check.");
            Console.WriteLine("  --with-includes                     Request include list while reading.");
            Console.WriteLine("  --title <text>                      Title used by abap-write.");
            Console.WriteLine("  --devclass <package>                Package/development class for abap-write, e.g. $TMP.");
            Console.WriteLine("  --transport <request>               Transport request for abap-write if required.");
            Console.WriteLine("  --temporary                         Set TEMPORARY=X in abap-write.");
            Console.WriteLine("  --save-inactive                     Set SAVE_INACTIVE=X in abap-write.");
            Console.WriteLine("  --dry-run                           Syntax-check only; do not write.");
            Console.WriteLine("  --confirm-write                     Required for any repository write.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SapProbe.exe system-info");
            Console.WriteLine("  SapProbe.exe table-read T000 --fields MANDT,MTEXT --where \"MANDT = '100'\" --rowcount 5");
            Console.WriteLine("  SapProbe.exe table-fields MARC MMSTA");
            Console.WriteLine("  SapProbe.exe field-exists MARC MMSTA");
            Console.WriteLine("  SapProbe.exe function-info RFC_SYSTEM_INFO");
            Console.WriteLine("  SapProbe.exe function-search RPY*PROGRAM*");
            Console.WriteLine("  SapProbe.exe rfc-call STFC_CONNECTION --set REQUTEXT=hello");
            Console.WriteLine("  SapProbe.exe abap-read Z_TEST3 --out C:\\Temp\\Z_TEST3.abap");
            Console.WriteLine("  SapProbe.exe abap-check Z_TEST3 --source-file C:\\Temp\\Z_TEST3.abap");
            Console.WriteLine("  SapProbe.exe abap-write Z_TEST3 --source-file C:\\Temp\\Z_TEST3.abap --confirm-write");
            Console.WriteLine("  SapProbe.exe abap-activate Z_TEST3 --dry-run");
            Console.WriteLine();
            Console.WriteLine("Password input:");
            Console.WriteLine("  This tool never accepts a password as a command-line argument.");
            Console.WriteLine("  It reads SAP_NCO_PASSWORD or SAP_T76_PASSWORD if set; otherwise it prompts with masking.");
        }
    }

    internal enum SapCommand
    {
        SystemInfo,
        TableRead,
        TableFields,
        FieldExists,
        FunctionInfo,
        FunctionSearch,
        RfcCall,
        AbapRead,
        AbapCheck,
        AbapWrite,
        AbapActivate,
        LoadOnly
    }

    internal enum OutputFormat
    {
        Table,
        Csv,
        Json
    }

    internal sealed class AbapSyntaxResult
    {
        public int ErrorSubrc { get; set; }
        public string ErrorInclude { get; set; }
        public int ErrorLine { get; set; }
        public int ErrorOffset { get; set; }
        public string ErrorWord { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; } = new List<string>();
    }

    internal sealed class CliOptions
    {
        private readonly List<string> _positionals = new List<string>();

        public string Name { get; private set; } = "T76";
        public string AppServerHost { get; private set; } = "travt762.sap.trafag.com";
        public string SystemNumber { get; private set; } = "00";
        public string Client { get; private set; } = "100";
        public string User { get; private set; } = "KOI";
        public string Language { get; private set; } = "DE";
        public string SapRouter { get; private set; }
        public string Trace { get; private set; }
        public bool Quiet { get; private set; }
        public bool NoPasswordPrompt { get; private set; }
        public bool ShowHelp { get; private set; }
        public SapCommand Command { get; private set; } = SapCommand.SystemInfo;

        public string TableName { get; private set; }
        public string FieldName { get; private set; }
        public List<string> Fields { get; private set; } = new List<string>();
        public List<string> WhereClauses { get; private set; } = new List<string>();
        public int RowCount { get; private set; } = 10;
        public int RowSkip { get; private set; }
        public string Delimiter { get; private set; } = "|";
        public OutputFormat OutputFormat { get; private set; } = OutputFormat.Table;

        public string FunctionName { get; private set; }
        public string SearchPattern { get; private set; }
        public Dictionary<string, string> SetValues { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool DumpImports { get; private set; }
        public int MaxTableRows { get; private set; } = 20;
        public string ProgramName { get; private set; }
        public string OutputPath { get; private set; }
        public string SourceFile { get; private set; }
        public string Title { get; private set; }
        public string DevelopmentClass { get; private set; }
        public string TransportNumber { get; private set; }
        public bool ConfirmWrite { get; private set; }
        public bool DryRun { get; private set; }
        public bool SaveInactive { get; private set; }
        public bool Temporary { get; private set; }
        public bool WithIncludeList { get; private set; }
        public bool ReadLatestVersion { get; private set; }

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();
            var commandExplicitlySet = false;

            for (var i = 0; i < (args ?? Array.Empty<string>()).Length; i++)
            {
                var arg = args[i];
                var lower = arg.ToLowerInvariant();

                switch (lower)
                {
                    case "--help":
                    case "-h":
                    case "/?":
                        options.ShowHelp = true;
                        break;
                    case "--ashost":
                        options.AppServerHost = RequireValue(args, ref i, arg);
                        break;
                    case "--sysnr":
                        options.SystemNumber = RequireValue(args, ref i, arg);
                        break;
                    case "--client":
                        options.Client = RequireValue(args, ref i, arg);
                        break;
                    case "--user":
                        options.User = RequireValue(args, ref i, arg);
                        break;
                    case "--lang":
                        options.Language = RequireValue(args, ref i, arg).ToUpperInvariant();
                        break;
                    case "--router":
                        options.SapRouter = RequireValue(args, ref i, arg);
                        break;
                    case "--trace":
                        options.Trace = RequireValue(args, ref i, arg);
                        break;
                    case "--quiet":
                        options.Quiet = true;
                        break;
                    case "--no-password-prompt":
                        options.NoPasswordPrompt = true;
                        break;
                    case "--load-only":
                        options.Command = SapCommand.LoadOnly;
                        commandExplicitlySet = true;
                        break;
                    case "--fields":
                        options.Fields.AddRange(SplitCsvList(RequireValue(args, ref i, arg)).Select(field => field.ToUpperInvariant()));
                        break;
                    case "--field":
                        options.FieldName = RequireValue(args, ref i, arg).ToUpperInvariant();
                        break;
                    case "--where":
                        options.WhereClauses.Add(RequireValue(args, ref i, arg));
                        break;
                    case "--rowcount":
                        options.RowCount = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                        break;
                    case "--rowskip":
                    case "--rowskips":
                        options.RowSkip = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                        break;
                    case "--delimiter":
                        options.Delimiter = RequireValue(args, ref i, arg);
                        if (options.Delimiter.Length != 1)
                        {
                            throw new ArgumentException("--delimiter must be exactly one character for RFC_READ_TABLE.");
                        }
                        break;
                    case "--format":
                        options.OutputFormat = ParseOutputFormat(RequireValue(args, ref i, arg));
                        break;
                    case "--set":
                    case "--param":
                        AddSetValue(options, RequireValue(args, ref i, arg));
                        break;
                    case "--dump-imports":
                        options.DumpImports = true;
                        break;
                    case "--max-table-rows":
                        options.MaxTableRows = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                        break;
                    case "--out":
                    case "--output":
                        options.OutputPath = RequireValue(args, ref i, arg);
                        break;
                    case "--source-file":
                    case "--file":
                        options.SourceFile = RequireValue(args, ref i, arg);
                        break;
                    case "--title":
                        options.Title = RequireValue(args, ref i, arg);
                        break;
                    case "--devclass":
                    case "--development-class":
                        options.DevelopmentClass = RequireValue(args, ref i, arg);
                        break;
                    case "--transport":
                    case "--transport-number":
                        options.TransportNumber = RequireValue(args, ref i, arg);
                        break;
                    case "--confirm-write":
                        options.ConfirmWrite = true;
                        break;
                    case "--dry-run":
                        options.DryRun = true;
                        break;
                    case "--save-inactive":
                        options.SaveInactive = true;
                        break;
                    case "--temporary":
                        options.Temporary = true;
                        break;
                    case "--with-includes":
                        options.WithIncludeList = true;
                        break;
                    case "--latest":
                        options.ReadLatestVersion = true;
                        break;
                    default:
                        if (arg.StartsWith("--", StringComparison.Ordinal))
                        {
                            throw new ArgumentException("Unknown option: " + arg);
                        }

                        if (!commandExplicitlySet && TryParseCommand(arg, out var command))
                        {
                            options.Command = command;
                            commandExplicitlySet = true;
                        }
                        else
                        {
                            options._positionals.Add(arg);
                        }
                        break;
                }
            }

            options.ApplyPositionals();
            return options;
        }

        private void ApplyPositionals()
        {
            switch (Command)
            {
                case SapCommand.TableRead:
                    TableName = TakeRequiredPositional(0, "table name");
                    break;
                case SapCommand.TableFields:
                    TableName = TakeRequiredPositional(0, "table name");
                    if (_positionals.Count > 1)
                    {
                        FieldName = _positionals[1].ToUpperInvariant();
                    }
                    break;
                case SapCommand.FieldExists:
                    TableName = TakeRequiredPositional(0, "table name");
                    FieldName = TakeRequiredPositional(1, "field name").ToUpperInvariant();
                    break;
                case SapCommand.FunctionInfo:
                case SapCommand.RfcCall:
                    FunctionName = TakeRequiredPositional(0, "function name");
                    break;
                case SapCommand.FunctionSearch:
                    SearchPattern = TakeRequiredPositional(0, "function search pattern");
                    break;
                case SapCommand.AbapRead:
                case SapCommand.AbapCheck:
                case SapCommand.AbapWrite:
                case SapCommand.AbapActivate:
                    ProgramName = TakeRequiredPositional(0, "ABAP program name");
                    break;
                case SapCommand.SystemInfo:
                case SapCommand.LoadOnly:
                    if (_positionals.Count > 0)
                    {
                        throw new ArgumentException("Unexpected positional argument: " + _positionals[0]);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var allowedPositionals = Command == SapCommand.TableFields || Command == SapCommand.FieldExists ? 2 : 1;
            if (_positionals.Count > allowedPositionals)
            {
                throw new ArgumentException("Unexpected positional argument: " + _positionals[allowedPositionals]);
            }
        }

        private string TakeRequiredPositional(int index, string description)
        {
            if (_positionals.Count <= index || string.IsNullOrWhiteSpace(_positionals[index]))
            {
                throw new ArgumentException("Missing " + description + ".");
            }

            return _positionals[index];
        }

        private static bool TryParseCommand(string text, out SapCommand command)
        {
            switch (text.ToLowerInvariant())
            {
                case "system-info":
                case "info":
                case "ping":
                    command = SapCommand.SystemInfo;
                    return true;
                case "table-read":
                case "read-table":
                case "table":
                    command = SapCommand.TableRead;
                    return true;
                case "table-fields":
                case "fields":
                case "ddic-fields":
                    command = SapCommand.TableFields;
                    return true;
                case "field-exists":
                case "has-field":
                    command = SapCommand.FieldExists;
                    return true;
                case "function-info":
                case "func-info":
                case "interface":
                    command = SapCommand.FunctionInfo;
                    return true;
                case "function-search":
                case "func-search":
                case "search-functions":
                    command = SapCommand.FunctionSearch;
                    return true;
                case "rfc-call":
                case "call":
                case "rfc":
                    command = SapCommand.RfcCall;
                    return true;
                case "abap-read":
                case "read-program":
                case "program-read":
                    command = SapCommand.AbapRead;
                    return true;
                case "abap-check":
                case "syntax-check":
                case "program-check":
                    command = SapCommand.AbapCheck;
                    return true;
                case "abap-write":
                case "write-program":
                case "program-write":
                    command = SapCommand.AbapWrite;
                    return true;
                case "abap-activate":
                case "activate-program":
                case "program-activate":
                    command = SapCommand.AbapActivate;
                    return true;
                case "load-only":
                    command = SapCommand.LoadOnly;
                    return true;
                default:
                    command = SapCommand.SystemInfo;
                    return false;
            }
        }

        private static string RequireValue(string[] args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing value for " + option);
            }

            index++;
            return args[index];
        }

        private static int ParseNonNegativeInt(string value, string option)
        {
            if (!int.TryParse(value, out var number) || number < 0)
            {
                throw new ArgumentException(option + " expects a non-negative integer.");
            }

            return number;
        }

        private static OutputFormat ParseOutputFormat(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "table":
                    return OutputFormat.Table;
                case "csv":
                    return OutputFormat.Csv;
                case "json":
                    return OutputFormat.Json;
                default:
                    throw new ArgumentException("Unsupported output format: " + value);
            }
        }

        private static IEnumerable<string> SplitCsvList(string value)
        {
            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        private static void AddSetValue(CliOptions options, string expression)
        {
            var separator = expression.IndexOf('=');
            if (separator < 1)
            {
                throw new ArgumentException("--set expects NAME=VALUE.");
            }

            var key = expression.Substring(0, separator).Trim();
            var value = expression.Substring(separator + 1);
            if (key.Length == 0)
            {
                throw new ArgumentException("--set expects NAME=VALUE.");
            }

            options.SetValues[key] = value;
        }

        public CliOptions CloneForActiveWrite()
        {
            return new CliOptions
            {
                Name = Name,
                AppServerHost = AppServerHost,
                SystemNumber = SystemNumber,
                Client = Client,
                User = User,
                Language = Language,
                SapRouter = SapRouter,
                Trace = Trace,
                Quiet = Quiet,
                NoPasswordPrompt = NoPasswordPrompt,
                Command = Command,
                ProgramName = ProgramName,
                SourceFile = SourceFile,
                Title = Title,
                DevelopmentClass = DevelopmentClass,
                TransportNumber = TransportNumber,
                ConfirmWrite = ConfirmWrite,
                DryRun = DryRun,
                SaveInactive = false,
                Temporary = Temporary
            };
        }
    }
}
