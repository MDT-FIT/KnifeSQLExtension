using DevToys.Api;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Features.RandomDataGeneration;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using static DevToys.Api.GUI;


namespace KnifeSQLExtension.UI.Views
{
    public class GenerationView : IView
    {
        private readonly SqlSession _session;
        private DependenciesService _dependenciesService;

        private readonly IUIButton _schemaButton = Button("btn-schema");
        private readonly IUIMultiLineTextInput _outputBox = MultiLineTextInput("sql-output");

        public GenerationView(SqlSession session)
        {
            _session = session;
        }

        public IUIElement View =>
            Stack()
                .Vertical()
                .WithChildren(
                    _outputBox
                        .Title("Result")
                        .Extendable()
                        .ReadOnly(),
                    Stack()
                        .Horizontal()
                        .WithChildren(
                            _schemaButton
                                .Text("Get Table Schema")
                                .AccentAppearance()
                                .OnClick(OnExecuteClicked)
                        )
        );

        private void OnExecuteClicked()
        {
            if(!_session.IsConnected || _session.DbClient is null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            _dependenciesService = new DependenciesService(_session.DbClient);

            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Виконання запиту...");

                    
                    List<string> chosenTables = ["Comments"];
                    List<string> affectedTables = [];

                    affectedTables = await _dependenciesService.GetDependenciesAsync(chosenTables);

                    var sb = new StringBuilder();
                    int index = 1;
                    foreach(var table in affectedTables)
                    {
                        sb.AppendLine($"{index}. {table}");
                        index++;
                    }

                    UpdateOutput(sb.ToString());

                }
                catch(Exception ex)
                {
                    UpdateOutput($"❌ Помилка SQL: {ex.Message}");
                }
            });

        }

        private void ShowTableSchema()
        {
            if(!_session.IsConnected || _session.DbClient == null)
            {
                UpdateOutput("⚠️ Будь ласка, спочатку підключіться до бази даних!");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    UpdateOutput("Виконання запиту...");
                    var result = await _session.DbClient.GetTableSchemaAsync("Orders");

                    var sb = new StringBuilder();

                    int colIndex = 1;
                    foreach(var col in result.Columns)
                    {
                        sb.AppendLine($"--- Результат {colIndex} ---");

                        sb.AppendLine($"{nameof(col.Name)}: {col.Name}");
                        sb.AppendLine($"{nameof(col.SqlType)}: {col.SqlType}");
                        sb.AppendLine($"{nameof(col.MaxLength)}: {(col.MaxLength is null ? "None" : col.MaxLength)}");
                        sb.AppendLine($"{nameof(col.IsNullable)}: {(col.IsNullable ? "Yes" : "No")}");
                        sb.AppendLine($"{nameof(col.IsPrimaryKey)}: {(col.IsPrimaryKey ? "Yes" : "No")}");
                        sb.AppendLine($"{nameof(col.IsIdentity)}: {(col.IsIdentity ? "Yes" : "No")}");
                        sb.AppendLine($"{nameof(col.IsComputed)}: {(col.IsComputed ? "Yes" : "No")}");
                        sb.AppendLine($"{nameof(col.HasDefault)}: {(col.HasDefault ? "Yes" : "No")}");

                        if(col.FkObject is not null)
                        {
                            sb.AppendLine($"Referenced table: {col.FkObject.FkTableName}");
                            sb.AppendLine($"Referenced column: {col.FkObject.FkColumnName}");
                        }

                        sb.AppendLine();
                        colIndex++;
                    }

                    UpdateOutput(sb.ToString());
                }
                catch(Exception ex)
                {
                    UpdateOutput($"❌ Помилка SQL: {ex.Message}");
                }
            });
        }

        private void UpdateOutput(string message) => _outputBox.Text(message);
    }
}
