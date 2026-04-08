using Bogus;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generator;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generators;
using SqlParser.Ast;


namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class GenerationService
    {
        private readonly IDatabaseClient _client;
        private readonly DependenciesService _dependenciesService;
        private readonly TableService _tableService;
        private readonly IGenerator? _generator;
        Dictionary<string, List<object>> pkTable = [];
        private readonly Faker _faker = new();

        public GenerationService(IDatabaseClient client, DependenciesService dependenciesService, TableService tableService)
        {
            _client = client;
            _dependenciesService = dependenciesService;
            _tableService = tableService;

            Type generatorType = client.Type switch
            {
                Core.Services.Database.Type.MsSql => typeof(MsSqlGenerator),
                Core.Services.Database.Type.MySql => typeof(MySqlGenerator),
                Core.Services.Database.Type.PostgreSql => typeof(PostgreSqlGenerator),
                _ => typeof(MsSqlGenerator),
            };
            _generator = Activator.CreateInstance(generatorType) as IGenerator;
        }

        public async Task Generate(List<string> tables, int rowCount)
        {
            Dictionary<string, List<Dictionary<string, object>>> generatedData = [];
            var schemas = await _tableService.GetTablesAsync();
            var sortedTables = await _dependenciesService.GetSortedTables();

            // For each table (already topologically sorted)
            sortedTables.Reverse();
            foreach(var table in sortedTables)
            {
                var rows = new List<Dictionary<string, object>>();
                if(tables.Contains(table))
                {
                    var schema = schemas.First(s => s.FullName == table);

                    for(int i = 0; i < rowCount; i++)
                    {
                        var row = new Dictionary<string, object>();
                        await GenerateTableData(schema, row);
                        rows.Add(row);
                    }
                    generatedData[table] = rows;
                }

                await _tableService.SeedTable(table, rows);
            }
        }

        private async Task GenerateTableData(TableSchema schema, Dictionary<string, object> row)
        {
            Dictionary<string, List<object>> uniqueColumnTable = [];
            foreach(var column in schema.Columns)
            {
                // Skip auto-managed columns
                if(column.IsIdentity || column.IsComputed)
                    continue;

                // FK column — sample from already generated keys
                if(column.FkObject is not null)
                {
                    var fkTable = column.FkObject.FkFullTableName;

                    if(!pkTable.ContainsKey(fkTable))
                    {
                        var fkSchema = (await _tableService.GetTablesAsync())
                            .First(s => s.FullName == fkTable);
                        var fkPkColumn = fkSchema.Columns.First(c => c.IsPrimaryKey);

                        var values = await GetColumnValues(fkTable, fkPkColumn);
                        pkTable[fkTable] = values;
                    }
                    var pool = pkTable.GetValueOrDefault(column.FkObject.FkFullTableName);
                    var value = (pool is null || pool.Count == 0)
                        ? null!
                        : _faker.PickRandom(pool);

                    pkTable[fkTable].Add(value);
                    row[column.Name] = value;

                    continue;
                }

                // Unique column
                if(column.IsUnique || column.IsPrimaryKey)
                {
                    if(!uniqueColumnTable.ContainsKey(column.Name))
                    {
                        var values = await GetColumnValues(schema.FullName, column);
                        uniqueColumnTable[column.Name] = values;
                    }
                    row[column.Name] = GenerateUniqueValue(_faker, column, uniqueColumnTable[column.Name]);
                }
                // Regular column — generate based on type/name
                row[column.Name] = _generator.GenerateValue(_faker, column);
            }
        }

        private async Task<List<object>> GetColumnValues(string table, ColumnSchema column)
        {
            var data = await _tableService.GetTableDataAsync(table);

            return [.. data
                .Where(entry => entry.ContainsKey(column.Name))
                .Select(entry => entry[column.Name])];
        }

        private object GenerateUniqueValue(Faker faker, ColumnSchema column, List<object> existingValues)
        {
            int maxAttempts = existingValues.Count;
            object value;

            while(maxAttempts > 0)
            {
                value = _generator.GenerateValue(faker, column);

                if(!existingValues.Contains(value))
                    return value;
            }

            return default!;
        }
    }
}
