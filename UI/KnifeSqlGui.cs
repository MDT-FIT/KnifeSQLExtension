using DevToys.Api;
using KnifeSQLExtension.Core;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;

namespace KnifeSQLExtension.UI;

[Export(typeof(IGuiTool))]
[Name("KnifeSQLExtension")] // A unique, internal name of the tool.
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons", // This font is available by default in DevToys
    IconGlyph = '\uE670', // An icon that represents a pizza
    GroupName = PredefinedCommonToolGroupNames.Converters, // The group in which the tool will appear in the sidebar.
    ResourceManagerAssemblyIdentifier = nameof(KnifeSqlAssemblyIdentifier), // The Resource Assembly Identifier to use
    ResourceManagerBaseName =
        "KnifeSQLExtension.KnifeSqlResources", // The full name (including namespace) of the resource file containing our localized texts
    ShortDisplayTitleResourceName =
        nameof(KnifeSqlResources.ShortDisplayTitle), // The name of the resource to use for the short display title
    LongDisplayTitleResourceName = nameof(KnifeSqlResources.LongDisplayTitle),
    DescriptionResourceName = nameof(KnifeSqlResources.Description),
    AccessibleNameResourceName = nameof(KnifeSqlResources.AccessibleName))]
internal sealed class KnifeSqlGui : IGuiTool
{
    // --- Elements of interface ---

    // Textfield for connection string
    private readonly IUIMultiLineTextInput _connectionStringInput = MultiLineTextInput("sql-connection-string");

    // Field for log's output
    private readonly IUIMultiLineTextInput _outputBox = MultiLineTextInput("sql-output");

    // --- State Management ---
    // We need to store the client at the class level to use it across different buttons
    private IDatabaseClient _dbClient;
    private bool _isConnected = false;

    // --- Visual part (View) ---
    public UIToolView View
        => new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    // Section 1: Connection
                    Label("Database Connection Test").Style(UILabelStyle.Subtitle),

                    _connectionStringInput
                        .Title("Connection String")
                        .Text("Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"),

                    Button("btn-connect")
                        .Text("1. Connect to Database")
                        .OnClick(OnConnectClicked),

                    // Section 2: CRUD Operations
                    Label("CRUD Operations").Style(UILabelStyle.Subtitle),

                    // Group buttons horizontally for better layout
                    Stack().Horizontal().WithChildren(
                        Button("btn-create-table").Text("2. Create Table").OnClick(OnCreateTableClicked),
                        Button("btn-insert").Text("3. Insert Data").OnClick(OnInsertClicked),
                        Button("btn-read").Text("4. Read Data").OnClick(OnReadClicked)
                    ),
                    Stack().Horizontal().WithChildren(
                        Button("btn-update").Text("5. Update Data").OnClick(OnUpdateClicked),
                        Button("btn-delete").Text("6. Delete Data").OnClick(OnDeleteClicked)
                    ),

                    // Section 3: Logs
                    Label("Status & Logs").Style(UILabelStyle.Subtitle),

                    _outputBox
                        .Title("Log Output")
                        .ReadOnly() // User here can only read
                ));

    // --- 1. CONNECT Logic ---
    private void OnConnectClicked()
    {
        Task.Run(async () =>
        {
            try
            {
                UpdateOutput("Attempting to connect...");

                string connString = _connectionStringInput.Text;

                // Initialize the client
                _dbClient = DbConnect.GetClient(DatabaseType.SqlServer);

                // Try to connect and store the result
                _isConnected = await _dbClient.ConnectAsync(connString);

                if (_isConnected)
                {
                    UpdateOutput("✅ Success! Connection established. You can now use the CRUD buttons below.");
                    // We DO NOT disconnect here anymore, so we can run other tests.
                }
                else
                {
                    UpdateOutput("❌ Connection failed. Check your Connection String.");
                }
            }
            catch (System.Exception ex)
            {
                UpdateOutput($"🔥 Error: {ex.Message}");
            }
        });
    }

    // --- 2. CREATE TABLE Logic ---
    private void OnCreateTableClicked()
    {
        RunTest(async () =>
        {
            // SQL query to create a table if it doesn't exist
            string query = "IF OBJECT_ID('TestUser', 'U') IS NULL CREATE TABLE TestUser (Id INT, Name NVARCHAR(50), Age INT)";
            await _dbClient.ExecuteQueryAsync(query);
            return "Table 'TestUser' created (or already exists).";
        });
    }

    // --- 3. INSERT Logic ---
    private void OnInsertClicked()
    {
        RunTest(async () =>
        {
            // Prepare data to insert
            var data = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Andrii" },
                { "Age", 20 }
            };
            await _dbClient.InsertDataAsync("TestUser", data);
            return "Row inserted: Id=1, Name=Andrii, Age=20";
        });
    }

    // --- 4. READ Logic ---
    private void OnReadClicked()
    {
        RunTest(async () =>
        {
            // Get all data from the table
            var results = await _dbClient.GetDataAsync("TestUser");

            string log = $"Rows found: {results.Count}\n";
            foreach (var row in results)
            {
                // Format the output string
                log += $" - User: {row["Name"]}, Age: {row["Age"]}\n";
            }
            return log;
        });
    }

    // --- 5. UPDATE Logic ---
    private void OnUpdateClicked()
    {
        RunTest(async () =>
        {
            var data = new Dictionary<string, object>
            {
                { "Name", "Andrii Updated" },
                { "Age", 25 }
            };
            // Update the row where Id = 1
            await _dbClient.UpdateDataAsync("TestUser", "Id", "1", data);
            return "Row updated (Id=1). Name is now 'Andrii Updated'.";
        });
    }

    // --- 6. DELETE Logic ---
    private void OnDeleteClicked()
    {
        RunTest(async () =>
        {
            // Delete the row where Id = 1
            await _dbClient.DeleteDataAsync("TestUser", "Id", "1");
            return "Row with Id=1 deleted.";
        });
    }

    // --- Helper method to run tests safely ---
    private void RunTest(System.Func<Task<string>> testAction)
    {
        if (!_isConnected || _dbClient == null)
        {
            UpdateOutput("⚠️ Please connect first!");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                string result = await testAction();
                UpdateOutput($"✅ {result}");
            }
            catch (System.Exception ex)
            {
                UpdateOutput($"🔥 Error: {ex.Message}");
            }
        });
    }

    // Additional method for safe updating text
    private void UpdateOutput(string message)
    {
        _outputBox.Text(message);
    }

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Interface demands this method
    }
}