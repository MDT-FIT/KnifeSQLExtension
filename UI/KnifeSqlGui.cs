using DevToys.Api;
using KnifeSQLExtension.Core;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI;

[Export(typeof(IGuiTool))]
[Name("KnifeSQLExtension")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE670',
    GroupName = PredefinedCommonToolGroupNames.Converters,
    ResourceManagerAssemblyIdentifier = nameof(KnifeSqlAssemblyIdentifier),
    ResourceManagerBaseName = "KnifeSQLExtension.KnifeSqlResources",
    ShortDisplayTitleResourceName = nameof(KnifeSqlResources.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(KnifeSqlResources.LongDisplayTitle),
    DescriptionResourceName = nameof(KnifeSqlResources.Description),
    AccessibleNameResourceName = nameof(KnifeSqlResources.AccessibleName))]
internal sealed class KnifeSqlGui : IGuiTool
{
    // --- UI Elements ---

    // ДОДАНО: Випадаючий список для вибору БД
    // Dropdown for selecting database type
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

    private IDatabaseClient _dbClient;
    private bool _isConnected = false;

    // --- View Layout (Використовуємо SplitGrid для великого Output) ---
    public UIToolView View
        => new UIToolView(
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
                                .Text("Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;Encrypt=False;"),
                            _connectButton.Text("Connect").OnClick(OnConnectClicked),

                            Label("Query Editor").Style(UILabelStyle.Subtitle),
                            _queryInput.Title("SQL Query").Language("sql").Text("SELECT * FROM INFORMATION_SCHEMA.TABLES;"),
                            _executeButton.Text("Execute Query").AccentAppearance().OnClick(OnExecuteClicked)
                        )
                )
                .WithBottomPaneChild(
                    // OutputBox розтягнеться на всю нижню панель!
                    _outputBox.Title("Results & Logs").ReadOnly()
                )
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

                _dbClient = DbConnect.GetClient(dbType);
                _isConnected = await _dbClient.ConnectAsync(_connectionStringInput.Text);

                if (_isConnected)
                    UpdateOutput($"✅ Підключено до {dbType} успішно!");
                else
                    UpdateOutput("❌ Підключитися не вдалося.");
            }
            catch (System.Exception ex) { UpdateOutput($"❌ Помилка: {ex.Message}"); }
        });
    }

    private void OnExecuteClicked()
    {
        if (!_isConnected || _dbClient == null)
        {
            UpdateOutput("⚠️ Спочатку підключіться до БД!");
            return;
        }

        string query = _queryInput.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        // parser integration
        string parserWarning = Parser.CheckForWarnings(query);
        if (!string.IsNullOrEmpty(parserWarning))
        {
            // if parser returned warning, block execution and demonstrate it
            UpdateOutput($"{parserWarning}\n\n❌ Виконання запиту скасовано через безпекові причини.");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                UpdateOutput("Виконання запиту...");
                var results = await _dbClient.ExecuteQueryAsync(query);

                var sb = new StringBuilder();
                sb.AppendLine($"✅ Запит успішно виконано. ({results.Count} результат(-ів) повернуто)\n");

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
    public void OnDataReceived(string dataTypeName, object? parsedData) { }
}