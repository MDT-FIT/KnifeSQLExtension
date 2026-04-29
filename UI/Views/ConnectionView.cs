using DevToys.Api;
using KnifeSQLExtension.Core;
using System.Text;
using KnifeSQLExtension.Core.Services;
using KnifeSQLExtension.Core.Services.Database;
using Microsoft.Extensions.Logging;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI.Views
{
    public sealed class ConnectionView : IView
    {
        private readonly SqlSession _session;
        private readonly ILogger<ConnectionView> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ConnectionView(SqlSession session, ILogger<ConnectionView> logger,
            ILoggerFactory loggerFactory)
        {
            _session = session;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        private readonly IUIMultiLineTextInput _connectionStringInput = MultiLineTextInput("sql-connection-string");
        private readonly IUIButton _connectButton = Button("btn-connect");

        private readonly IUIMultiLineTextInput _queryInput = MultiLineTextInput("sql-query");
        private readonly IUIButton _executeButton = Button("btn-execute");

        private readonly IUIMultiLineTextInput _outputBox = MultiLineTextInput("sql-output");

        // --- State variables for Confirmation Logic ---
        private bool _isAwaitingConfirmation = false;
        private string _pendingDangerousQuery = string.Empty;

        public IUIElement View =>
            SplitGrid()
                .Vertical()
                .TopPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                .BottomPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                .WithTopPaneChild(
                    Stack()
                        .Vertical()
                        .WithChildren(
                            Label("Database Connection").Style(UILabelStyle.Subtitle),
                            Stack()
                                .Horizontal() ,
                            _connectionStringInput
                                .Title("Connection String")
                                .Text("Server=DESKTOP-J1QI1UR;Database=test;Integrated Security=True;Encrypt=False;"),
                            _connectButton.Text("Connect").OnClick(OnConnectClicked),
                            Label("Query Editor").Style(UILabelStyle.Subtitle),
                            _queryInput.Title("SQL Query").Language("sql")
                                .Text("SELECT * FROM INFORMATION_SCHEMA.TABLES;"),
                            _executeButton.Text("Execute Query").AccentAppearance().OnClick(OnExecuteClicked)
                        )
                )
                .WithBottomPaneChild(
                    _outputBox.Title("Results & Logs").ReadOnly()
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
                        UpdateOutput($"✅ Підключено до MS SQL успішно!");
                    }
                    else
                        UpdateOutput("❌ Підключитися не вдалося.");
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

            // 1. Check if we wait this one query 
            if (_isAwaitingConfirmation && query == _pendingDangerousQuery)
            {
                // User pressed the button secondly
                _isAwaitingConfirmation = false;
                _pendingDangerousQuery = string.Empty;
                // Return usual button's text
                _executeButton.Text("Виконати запит");

                UpdateOutput("⚠️ Виконання небезпечного запиту після підтвердження користувачем...");
            }
            else
            {
                // 2. Usual flow - check query with parser
                string parserWarning = Parser.CheckForWarnings(query);
                if (!string.IsNullOrEmpty(parserWarning))
                {
                    // Parser find out the danger
                    _isAwaitingConfirmation = true;
                    _pendingDangerousQuery = query;

                    // Change button text with warning
                    _executeButton.Text("⚠️ ПІДТВЕРДИТИ ЗАПИТ ⚠️");
                    UpdateOutput(
                        $"{parserWarning}\n\n🛑 Запит призупинено для вашої безпеки.\nЯкщо ви ДІЙСНО хочете його виконати, натисніть кнопку '⚠️ ПІДТВЕРДИТИ ЗАПИТ ⚠️' ще раз.");
                    return;
                }

                // If query is safe, change the state
                _isAwaitingConfirmation = false;
                _pendingDangerousQuery = string.Empty;
                _executeButton.Text("Виконати запит");
            }

            // Query execution
            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Виконання запиту...");
                    var results = await _session.GetDbClient().ExecuteQueryAsync(query);

                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ Запит успішно виконано. (Повернуто результатів: {results.Count})\n");

                    int rowIndex = 1;
                    foreach (var row in results)
                    {
                        sb.AppendLine($"--- Результат {rowIndex} ---");
                        foreach (var column in row)
                        {
                            string value = column.Value == System.DBNull.Value
                                ? "NULL"
                                : column.Value?.ToString() ?? "NULL";
                            sb.AppendLine($"{column.Key}: {value}");
                        }

                        sb.AppendLine();
                        rowIndex++;
                    }

                    UpdateOutput(sb.ToString());
                }
                catch (System.Exception ex)
                {
                    UpdateOutput($"❌ Помилка SQL: {ex.Message}");
                }
            });
        }

        private void UpdateOutput(string message) => _outputBox.Text(message);
    }
}