using System.Drawing;
using System.Text;

namespace DeployConsole;

public sealed class MainForm : Form
{
    private readonly DeploySettings _settings;
    private readonly string _settingsPath;

    private readonly TextBox _target = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _project = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _title = new() { Dock = DockStyle.Fill };
    private readonly TextBox _commit = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _testsGreen = new() { Text = "Tests gruen (Release-Lauf vor dem Publish)", AutoSize = true };
    private readonly TextBox _testCount = new() { Width = 90, Text = "" };
    private readonly CheckBox _smoke = new() { Text = "Seiten abrufen", AutoSize = true, Checked = true };
    private readonly CheckBox _dryRun = new() { Text = "Nur Prueflauf (kein Publish)", AutoSize = true, Checked = true };
    private readonly TextBox _expected = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F) };
    private readonly TextBox _forbidden = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F) };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F), WordWrap = false };
    private readonly TextBox _protocol = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F), WordWrap = false };
    private readonly Button _run = new() { Text = "Deploy starten", AutoSize = true };
    private readonly Button _copy = new() { Text = "Protokoll kopieren", AutoSize = true, Enabled = false };
    private readonly Label _status = new() { Text = "Bereit.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _protocolTab = new("Protokoll");

    private CancellationTokenSource? _cts;

    public MainForm()
    {
        Text = "BiDashboard Deploy";
        Width = 1000;
        Height = 780;
        MinimumSize = new Size(820, 620);
        StartPosition = FormStartPosition.CenterScreen;

        _settingsPath = DeploySettings.DefaultPath;
        _settings = DeploySettings.Load(_settingsPath);
        _target.Text = _settings.TargetDir;
        _project.Text = _settings.ProjectPath;

        BuildLayout();
        _run.Click += async (_, _) => await RunAsync();
        _copy.Click += (_, _) =>
        {
            if (_protocol.TextLength > 0)
            {
                Clipboard.SetText(_protocol.Text);
                _status.Text = "Protokoll in der Zwischenablage - gehoert nach docs/rag/DEPLOYMENT.md und lastchange.md.";
            }
        };
        _dryRun.CheckedChanged += (_, _) => UpdateRunButton();
        UpdateRunButton();
    }

    private void BuildLayout()
    {
        var head = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8, 8, 8, 0) };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(string label, Control control)
        {
            head.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            head.Controls.Add(control);
        }
        Row("Ziel:", _target);
        Row("Projekt:", _project);
        Row("Titel:", _title);
        Row("Commit:", _commit);

        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(8, 6, 8, 0) };
        _testsGreen.Margin = new Padding(0, 6, 6, 3);
        options.Controls.Add(_testsGreen);
        options.Controls.Add(new Label { Text = "Anzahl:", AutoSize = true, Margin = new Padding(6, 6, 3, 3) });
        _testCount.Margin = new Padding(0, 3, 18, 3);
        options.Controls.Add(_testCount);
        _smoke.Margin = new Padding(0, 6, 18, 3);
        options.Controls.Add(_smoke);
        _dryRun.Margin = new Padding(0, 6, 3, 3);
        options.Controls.Add(_dryRun);

        var needles = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8, 6, 8, 0) };
        needles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        needles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        needles.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        needles.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        needles.Controls.Add(new Label { Text = "Muss in der DLL stehen (je Zeile ein Typ/Text):", AutoSize = true }, 0, 0);
        needles.Controls.Add(new Label { Text = "Darf NICHT mehr drinstehen:", AutoSize = true }, 1, 0);
        needles.Controls.Add(_expected, 0, 1);
        needles.Controls.Add(_forbidden, 1, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(8, 6, 8, 4) };
        buttons.Controls.Add(_run);
        _copy.Margin = new Padding(12, 3, 3, 3);
        buttons.Controls.Add(_copy);

        var logTab = new TabPage("Ablauf");
        logTab.Controls.Add(_log);
        _protocolTab.Controls.Add(_protocol);
        _tabs.TabPages.Add(logTab);
        _tabs.TabPages.Add(_protocolTab);

        var statusPanel = new Panel { Dock = DockStyle.Fill, Height = 26, Padding = new Padding(8, 2, 8, 4) };
        statusPanel.Controls.Add(_status);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.Controls.Add(head, 0, 0);
        root.Controls.Add(options, 0, 1);
        root.Controls.Add(needles, 0, 2);
        root.Controls.Add(buttons, 0, 3);
        root.Controls.Add(_tabs, 0, 4);
        root.Controls.Add(statusPanel, 0, 5);
        Controls.Add(root);
    }

    private void UpdateRunButton()
    {
        _run.Text = _dryRun.Checked ? "Prueflauf starten" : "Deploy starten (schreibt ins Ziel)";
        _run.BackColor = _dryRun.Checked ? SystemColors.Control : Color.FromArgb(255, 226, 226);
    }

    private async Task RunAsync()
    {
        if (!_dryRun.Checked)
        {
            var confirm = MessageBox.Show(
                this,
                $"Es wird nach\n\n{_settings.TargetDir}\n\npubliziert. Die Anwendung geht kurz offline.\n\nFortfahren?",
                "Produktiv deployen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
            if (!_testsGreen.Checked)
            {
                var anyway = MessageBox.Show(
                    this,
                    "Der Testlauf ist nicht bestaetigt. Das Protokoll haelt das dann ausdruecklich fest.\n\nTrotzdem deployen?",
                    "Ohne bestaetigte Tests",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (anyway != DialogResult.Yes)
                {
                    return;
                }
            }
        }

        _log.Clear();
        _protocol.Clear();
        _copy.Enabled = false;
        _run.Enabled = false;
        _status.Text = _dryRun.Checked ? "Prueflauf laeuft..." : "Deploy laeuft...";
        _cts = new CancellationTokenSource();

        var request = new DeployRequest
        {
            Title = _title.Text.Trim(),
            Commit = _commit.Text.Trim(),
            TestsGreen = _testsGreen.Checked,
            TestCount = _testCount.Text.Trim(),
            Expected = SplitLines(_expected.Text),
            Forbidden = SplitLines(_forbidden.Text),
            RunSmokeTests = _smoke.Checked,
            DryRun = _dryRun.Checked,
        };

        try
        {
            var runner = new DeployRunner(_settings, Append);
            var report = await Task.Run(() => runner.RunAsync(request, _cts.Token), _cts.Token);
            _protocol.Text = ProtocolWriter.Build(request, report).Replace("\n", Environment.NewLine);
            _copy.Enabled = true;
            _tabs.SelectedTab = _protocolTab;
            _status.Text = report.Succeeded
                ? "Ohne Alarm durchgelaufen. Protokoll steht bereit."
                : $"{report.Alarms.Count} Alarm(e) - siehe Ablauf und Protokoll.";
        }
        catch (Exception ex)
        {
            Append("ABBRUCH: " + ex.Message);
            _status.Text = "Abgebrochen: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Abgebrochen", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _run.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static List<string> SplitLines(string text) => text
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Trim())
        .Where(l => l.Length > 0)
        .ToList();

    private void Append(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(Append), line);
            return;
        }
        _log.AppendText(line + Environment.NewLine);
    }
}
