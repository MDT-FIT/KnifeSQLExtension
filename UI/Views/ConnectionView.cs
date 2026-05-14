using DevToys.Api;
using KnifeSQLExtension.Core;
using KnifeSQLExtension.Core.Services;
using KnifeSQLExtension.Core.Services.Database;
using Microsoft.Extensions.Logging;
using System.Text;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI.Views
{
    public sealed class ConnectionView : IView
    {
        private readonly SqlSession _session;
        private readonly ILogger<ConnectionView> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly SnapshotService _snapshotService;

        public ConnectionView(SqlSession session, ILogger<ConnectionView> logger,
            ILoggerFactory loggerFactory, SnapshotService snapshotService)
        {
            _session = session;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _snapshotService = snapshotService;
        }

        private readonly IUIMultiLineTextInput _connectionStringInput = MultiLineTextInput("sql-connection-string");
        private readonly IUIButton _connectButton = Button("btn-connect");
        private readonly IUIMultiLineTextInput _queryInput = MultiLineTextInput("sql-query");
        private readonly IUIButton _executeButton = Button("btn-execute");
        private readonly IUIMultiLineTextInput _outputBox = MultiLineTextInput("sql-output");
        private readonly IUIMultiLineTextInput _diffBox = MultiLineTextInput("sql-diff");
        private readonly IUISelectDropDownList _tableSelector = SelectDropDownList("snapshot-table-selector");

        private bool _isAwaitingConfirmation = false;
        private string _pendingDangerousQuery = string.Empty;

        private readonly IUIWebView _snapshotWebView = WebView();
        private readonly IUIWebView _currentWebView = WebView();
        private LocalServer? _diffServer;
        private List<DiffCell> _lastDiffs = new();
        private List<Dictionary<string, object>> _lastSnapshotRows = new();
        private List<Dictionary<string, object>> _lastCurrentRows = new();
        private List<string> _lastColumns = new();

        public IUIElement View =>
            Stack()
                .Vertical()
                .WithChildren(
                    // Блок 1: Connection + Query
                    Stack()
                        .Vertical()
                        .WithChildren(
                            Label("Database Connection").Style(UILabelStyle.Subtitle),
                            Stack().Horizontal(),
                            _connectionStringInput
                                .Title("Connection String")
                                .Text("Server=DESKTOP-O3CE97K;Database=knifetest;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"),
                            _connectButton.Text("Connect").OnClick(OnConnectClicked),
                            Label("Query Editor").Style(UILabelStyle.Subtitle),
                            _queryInput.Title("SQL Query").Language("sql")
                                .Text("SELECT * FROM INFORMATION_SCHEMA.TABLES;"),
                            _executeButton.Text("Execute Query").AccentAppearance().OnClick(OnExecuteClicked)
                        ),

                    // Блок 2: Results & Logs
                    _outputBox.Title("Results & Logs").ReadOnly(),

                    // Блок 3: Snapshot таблиці
                    Label("Database Snapshot Diff").Style(UILabelStyle.Subtitle),
                    Stack()
                        .Horizontal()
                        .WithChildren(
                            Label("Table:"),
                            _tableSelector.OnItemSelected(OnTableSelected)
                        ),
                    SplitGrid()
                        .Horizontal()
                        .LeftPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                        .RightPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                        .WithLeftPaneChild(
                            _snapshotWebView
                                .AlignVertically(UIVerticalAlignment.Stretch)
                                .AlignHorizontally(UIHorizontalAlignment.Stretch)
                        )
                        .WithRightPaneChild(
                            _currentWebView
                                .AlignVertically(UIVerticalAlignment.Stretch)
                                .AlignHorizontally(UIHorizontalAlignment.Stretch)
                        ),

                    // Блок 4: Changed cells
                    _diffBox.Title("Changed cells").ReadOnly()
                );

        private void OnConnectClicked()
        {
            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Підключення...");

                    var type = ConnectionStringParser.ParseConnectionString(_connectionStringInput.Text);
                    var client = DatabaseFactory.CreateDatabaseClient(type, _loggerFactory);
                    var isConnected = await client.ConnectAsync(_connectionStringInput.Text);
                    _session.ConnectDbClient(client, isConnected);

                    if (_session.IsConnected)
                    {
                        UpdateOutput("✅ Підключено успішно! Знімаємо снапшот БД...");
                        await _snapshotService.CaptureAllSnapshotsAsync(client);
                        RefreshTableSelector();
                        StartDiffServer();
                        UpdateOutput($"✅ Підключено! Снапшот збережено для {_snapshotService.GetSnapshotTableNames().Count} таблиць.");
                    }
                    else
                    {
                        UpdateOutput("❌ Підключитися не вдалося.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to database");
                    UpdateOutput($"❌ Помилка: {ex.Message}");
                }
            });
        }

        private void OnExecuteClicked()
        {
            if (!_session.IsConnected || _session.GetDbClient() == null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            string query = _queryInput.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            if (_isAwaitingConfirmation && query == _pendingDangerousQuery)
            {
                _isAwaitingConfirmation = false;
                _pendingDangerousQuery = string.Empty;
                _executeButton.Text("Execute Query");
                UpdateOutput("⚠️ Виконання небезпечного запиту після підтвердження...");
            }
            else
            {
                string parserWarning = Parser.CheckForWarnings(query);
                if (!string.IsNullOrEmpty(parserWarning))
                {
                    _isAwaitingConfirmation = true;
                    _pendingDangerousQuery = query;
                    _executeButton.Text("⚠️ ПІДТВЕРДИТИ ЗАПИТ ⚠️");
                    UpdateOutput($"{parserWarning}\n\n🛑 Запит призупинено.\nНатисніть '⚠️ ПІДТВЕРДИТИ ЗАПИТ ⚠️' ще раз для виконання.");
                    return;
                }

                _isAwaitingConfirmation = false;
                _pendingDangerousQuery = string.Empty;
                _executeButton.Text("Execute Query");
            }

            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Виконання запиту...");
                    var client = _session.GetDbClient()!;
                    var results = await client.ExecuteQueryAsync(query);

                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ Запит виконано. (Рядків: {results.Count})\n");
                    int rowIndex = 1;
                    foreach (var row in results)
                    {
                        sb.AppendLine($"--- Результат {rowIndex} ---");
                        foreach (var column in row)
                        {
                            string value = column.Value == DBNull.Value ? "NULL" : column.Value?.ToString() ?? "NULL";
                            sb.AppendLine($"{column.Key}: {value}");
                        }
                        sb.AppendLine();
                        rowIndex++;
                    }
                    UpdateOutput(sb.ToString());
                    await RefreshDiffAsync();
                }
                catch (Exception ex)
                {
                    UpdateOutput($"❌ Помилка SQL: {ex.Message}");
                }
            });
        }

        private void OnTableSelected(IUIDropDownListItem? item)
        {
            if (item == null) return;
            Task.Run(async () => await RefreshDiffAsync());
        }

        private async Task RefreshDiffAsync()
        {
            var client = _session.GetDbClient();
            if (client == null) return;

            string? tableName = _tableSelector.SelectedItem?.Value?.ToString();
            if (string.IsNullOrEmpty(tableName)) return;

            var snapshot = _snapshotService.GetSnapshot(tableName);
            if (snapshot == null) { _diffBox.Text("(немає снапшоту)"); return; }

            var currentRows = await client.GetDataAsync(tableName);
            var currentColumns = currentRows.Count > 0 ? currentRows[0].Keys.ToList() : snapshot.Columns;

            var diffs = _snapshotService.ComputeDiff(snapshot, currentRows);
            _lastDiffs = diffs;
            _lastSnapshotRows = snapshot.Rows;
            _lastCurrentRows = currentRows;
            _lastColumns = currentColumns;

            _diffBox.Text(RenderDiff(diffs));
            _snapshotWebView.NavigateToUri(new Uri("http://127.0.0.1:8081/snapshot"));
            _currentWebView.NavigateToUri(new Uri("http://127.0.0.1:8081/current"));
        }

        private void RefreshTableSelector()
        {
            var tableNames = _snapshotService.GetSnapshotTableNames();
            var items = tableNames.Select(t => Item(t, t)).ToArray();
            _tableSelector.WithItems(items);
            if (items.Length > 0)
                _tableSelector.Select(0);
        }

        private static string RenderDiff(List<DiffCell> diffs)
        {
            if (diffs.Count == 0) return "✅ Змін не виявлено.";

            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Знайдено змін: {diffs.Count}");
            sb.AppendLine();

            foreach (var diff in diffs)
            {
                string icon = diff.DiffType switch
                {
                    DiffType.Added => "➕",
                    DiffType.Deleted => "❌",
                    _ => "✏️"
                };
                sb.AppendLine($"{icon} Рядок {diff.RowIndex + 1}, колонка [{diff.ColumnName}]: \"{diff.OldValue}\" → \"{diff.NewValue}\"");
            }

            return sb.ToString();
        }

        private void UpdateOutput(string message) => _outputBox.Text(message);

        private void StartDiffServer()
        {
            _diffServer = new LocalServer("", _logger, 8081);

            _diffServer.RegisterEndpoint("/api/snapshot", () =>
            {
                if (_lastColumns.Count == 0) return "{}";
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append($"\"columns\":{System.Text.Json.JsonSerializer.Serialize(_lastColumns)},");
                sb.Append($"\"rows\":{SerializeRows(_lastSnapshotRows, _lastColumns)}");
                sb.Append("}");
                return sb.ToString();
            });

            _diffServer.RegisterEndpoint("/api/current", () =>
            {
                if (_lastColumns.Count == 0) return "{}";
                var diffsJson = System.Text.Json.JsonSerializer.Serialize(
                    _lastDiffs.Select(d => new { d.RowIndex, d.ColumnName, d.DiffType }).ToList()
                );
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append($"\"columns\":{System.Text.Json.JsonSerializer.Serialize(_lastColumns)},");
                sb.Append($"\"rows\":{SerializeRows(_lastCurrentRows, _lastColumns)},");
                sb.Append($"\"diffs\":{diffsJson}");
                sb.Append("}");
                return sb.ToString();
            });

            _diffServer.RegisterEndpoint("/snapshot", () => LoadHtml("Snapshot.html"));
            _diffServer.RegisterEndpoint("/current", () => LoadHtml("Current.html"));
            _diffServer.Start();

            _snapshotWebView.NavigateToUri(new Uri("http://127.0.0.1:8081/snapshot"));
            _currentWebView.NavigateToUri(new Uri("http://127.0.0.1:8081/current"));
        }

        private string SerializeRows(List<Dictionary<string, object>> rows, List<string> columns)
        {
            if (columns.Count == 0) return "[]";
            var result = rows.Select(row =>
                columns.Select(col =>
                {
                    if (!row.TryGetValue(col, out var val)) return "NULL";
                    return val == DBNull.Value || val == null ? "NULL" : val.ToString()!;
                }).ToList()
            ).ToList();
            return System.Text.Json.JsonSerializer.Serialize(result);
        }

        private static string LoadHtml(string fileName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName = $"KnifeSQLExtension.UI.DiffViews.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return "<html><body>File not found</body></html>";
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}