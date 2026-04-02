using DevToys.Api;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using static DevToys.Api.GUI;


namespace KnifeSQLExtension.UI.Views
{
    /// <summary>
    /// Represents a user interface view for selecting database schemas and tables, and for displaying related logs and
    /// actions within a SQL session.
    /// </summary>
    /// <remarks>The GenerationView provides interactive controls for refreshing the list of tables, selecting
    /// schemas, choosing tables, and viewing log output. It is designed to be used as part of a database tooling
    /// workflow where users need to browse and select database objects. The view requires an active database connection
    /// to function fully; certain actions are disabled or display warnings if the session is not connected.</remarks>
    public class GenerationView : IView
    {
        // Shared session 
        private readonly SqlSession _session;

        // Services
        private DependenciesService _dependenciesService;
        private TableService _tableService;

        // Table action buttons
        private readonly IUIButton _refreshButton = Button().Text("Refresh").AccentAppearance();
        private readonly IUIButton _selectAllButton = Button().Text("Select All").AccentAppearance();

        // Output box
        private readonly IUIMultiLineTextInput _outputBox = MultiLineTextInput().ReadOnly().Title("Logs");

        // Tables container
        private readonly IUIWrap _allTableWrap = Wrap();

        // Database schemas select
        private readonly IUISelectDropDownList _schemaSelect = SelectDropDownList().WithItems([Item("dbo")]).Select(0);

        public GenerationView(SqlSession session)
        {
            _session = session;
        }

        /// <summary>
        /// Gets the root user interface element for this component.
        /// </summary>
        public IUIElement View => BuildUI();


        /// <summary>
        /// Builds and returns the user interface layout for the schema and table selection view, including controls for
        /// refreshing tables, selecting schemas, choosing tables, and viewing logs.
        /// </summary>
        /// <remarks>The returned UI element includes interactive controls for refreshing the table list,
        /// selecting all tables, choosing a schema, and viewing logs. The layout is organized with clear separation
        /// between selection controls and output display.</remarks>
        /// <returns>An <see cref="IUIElement"/> representing the complete UI layout for the schema and table selection workflow.</returns>
        private IUIElement BuildUI()
        {
            return Stack()
                .LargeSpacing()
                .Vertical()
                .WithChildren(
                    SplitGrid()
                        .Vertical()
                        .WithLeftPaneChild(
                            Stack()
                                .LargeSpacing()
                                .Vertical()
                                .WithChildren(
                                    Stack()
                                        .LargeSpacing()
                                        .Horizontal()
                                        .WithChildren(
                                            _refreshButton
                                                .OnClick(OnRefreshClicked),
                                            _selectAllButton
                                                .OnClick(OnSelectAllClicked)
                                        ),
                                     Stack()
                                         .LargeSpacing()
                                         .Vertical()
                                         .WithChildren(
                                            Label()
                                            .Text("Select Schema"),
                                            _schemaSelect,
                                            Label()
                                            .Text("Choose tables"),
                                            _allTableWrap
                                         )))
                        .WithRightPaneChild(
                            _outputBox
                        ));
        }

        /// <summary>
        /// Handles the Select All action by asynchronously retrieving all tables for the selected schema and updating
        /// the UI to display a button for each table.
        /// </summary>
        /// <remarks>This method initiates an asynchronous operation to fetch tables and update the UI. It
        /// should be called in response to a user action, such as clicking a 'Select All' button. The method does not
        /// block the calling thread.</remarks>
        private void OnSelectAllClicked()
        {
            Task.Run(async () =>
            {
                var tables = await _tableService.GetTablesAsync(_schemaSelect.SelectedItem.Text);
                _allTableWrap.WithChildren([.. tables.Select((s) => {
                    var button = Button(s.TableName + "-btn", s.TableName);
                    button.AccentAppearance();

                    return button;
                })]);
            });
        }

        /// <summary>
        /// Handles the refresh action by reloading schema and table information if the session is connected to the
        /// database.
        /// </summary>
        /// <remarks>If the session is not connected to a database, this method displays a warning message
        /// and does not perform a refresh. This method is typically invoked in response to a user-initiated refresh
        /// command, such as clicking a refresh button in the UI.</remarks>
        private void OnRefreshClicked()
        {
            if(!_session.IsConnected || _session.DbClient is null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            Task.Run(async () =>
            {
                await InitSchemas();
                await RefreshTables();
            });

        }

        /// <summary>
        /// Initializes the required services and loads schema and table metadata asynchronously.
        /// </summary>
        /// <remarks>Call this method before performing operations that depend on schema or table
        /// information. This method must be awaited to ensure that all dependencies are fully initialized.</remarks>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        public async Task Init()
        {
            _dependenciesService = new DependenciesService(_session.DbClient);
            _tableService = new TableService(_session.DbClient);

            await InitSchemas();
            await RefreshTables();
        }

        /// <summary>
        /// Initializes the list of available database schemas by retrieving them from the connected database and
        /// updates the schema selection UI.
        /// </summary>
        /// <remarks>This method does nothing if there is no active database connection. Ensure that the
        /// session is connected before calling this method.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitSchemas()
        {
            if(!_session.IsConnected || _session.DbClient is null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }
            var dbSchemas = await _session.DbClient.GetDatabaseSchemasAsync();

            _schemaSelect.WithItems([.. dbSchemas.Select(t => Item(t))]);
        }

        /// <summary>
        /// Asynchronously refreshes the list of database tables displayed in the user interface based on the currently
        /// selected schema.
        /// </summary>
        /// <remarks>If there is no active database connection, the method does not perform the refresh
        /// and instead prompts the user to connect to the database first.</remarks>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        public async Task RefreshTables()
        {
            if(!_session.IsConnected || _session.DbClient is null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            var tables = await _tableService.GetTablesAsync(_schemaSelect.SelectedItem.Text);
            _allTableWrap.WithChildren([.. tables.Select((s) => {
                var button = Button(s.TableName + "-btn", s.TableName);

                button.OnClick(() => {
                    button.AccentAppearance();
                });

                return button;
            })]);
        }

        /// <summary>
        /// Updates the output display with the specified message.
        /// </summary>
        /// <param name="message">The message text to display in the output box. Cannot be null.</param>
        private void UpdateOutput(string message) => _outputBox.Text(message);
    }
}
