using DevToys.Api;
using KnifeSQLExtension.Core;
using System.Text;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI.Views
{
    public sealed class ConnectionView : IView
    {
        private readonly SqlSession _session;
        private readonly IUIButton _generationButton;
        private readonly GenerationView _generationView;

        public ConnectionView(SqlSession session, IUIButton generationButton, GenerationView generationView)
        {
            _session = session;
            _generationButton = generationButton;
            _generationView = generationView;
        }

        private readonly IUISelectDropDownList _dbTypeSelect = SelectDropDownList("db-type-select")
        .WithItems(
            Item("MS SQL Server", "sqlserver"),
            Item("PostgreSQL", "postgresql"),
            Item("MySQL", "mysql")
        )
        .Select(0);

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
                            _dbTypeSelect.Title("Database Type"), // Наш новий список
                            _connectionStringInput
                                .Title("Connection String")
                                .Text("Server=DESKTOP-J1QI1UR;Database=test;Integrated Security=True;Encrypt=False;"),
                            _connectButton.Text("Connect").OnClick(OnConnectClicked),

                            Label("Query Editor").Style(UILabelStyle.Subtitle),
                            _queryInput.Title("SQL Query").Language("sql").Text("SELECT * FROM INFORMATION_SCHEMA.TABLES;"),
                            _executeButton.Text("Execute Query").AccentAppearance().OnClick(OnExecuteClicked)
                        )
                )
                .WithBottomPaneChild(
                    // OutputBox розтягнеться на всю нижню панель!
                    _outputBox.Title("Results & Logs").ReadOnly()
        );

        // --- Logics ---
        private void OnConnectClicked()
        {
            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Підключення...");

                    // Визначаємо, яку БД обрав користувач
                    string selectedDb = _dbTypeSelect.SelectedItem?.Value?.ToString();
                    DatabaseType dbType = selectedDb switch
                    {
                        "postgresql" => DatabaseType.PostgreSql,
                        "mysql" => DatabaseType.MySql,
                        _ => DatabaseType.SqlServer
                    };

                    _session.DbClient = DbConnect.GetClient(dbType);
                    _session.IsConnected = await _session.DbClient.ConnectAsync(_connectionStringInput.Text);

                    if (_session.IsConnected)
                    {
                        UpdateOutput($"✅ Підключено до {dbType} успішно!");
                        await _generationView.Init();
                        _generationButton.Show();
                    }
                    else
                        UpdateOutput("❌ Підключитися не вдалося.");
                }
                catch (System.Exception ex) { UpdateOutput($"❌ Помилка: {ex.Message}"); }
            });
        }

        private void OnExecuteClicked()
        {
            if (!_session.IsConnected || _session.DbClient == null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            string query = _queryInput.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            // Parser with confirmation

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

                    UpdateOutput($"{parserWarning}\n\n🛑 Запит призупинено для вашої безпеки.\nЯкщо ви ДІЙСНО хочете його виконати, натисніть кнопку '⚠️ ПІДТВЕРДІТЬ НЕБЕЗПЕЧНИЙ ЗАПИТ ⚠️' ще раз.");
                    return;
                }

                // If query is safe, change the state
                _isAwaitingConfirmation = false;
                _pendingDangerousQuery = string.Empty;
                _executeButton.Text("Виконати запит");
            }

            // Querry execution
            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Виконання запиту...");
                    var results = await _session.DbClient.ExecuteQueryAsync(query);

                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ Запит успішно виконано. (Повернуто результатів: {results.Count})\n");

                    int rowIndex = 1;
                    foreach (var row in results)
                    {
                        sb.AppendLine($"--- Результат {rowIndex} ---");
                        foreach (var column in row)
                        {
                            string value = column.Value == System.DBNull.Value ? "NULL" : column.Value?.ToString() ?? "NULL";
                            sb.AppendLine($"{column.Key}: {value}");
                        }
                        sb.AppendLine();
                        rowIndex++;
                    }

                    UpdateOutput(sb.ToString());
                }
                catch (System.Exception ex) { UpdateOutput($"❌ Помилка SQL: {ex.Message}"); }
            });
        }

        private void UpdateOutput(string message) => _outputBox.Text(message);
    }
}
