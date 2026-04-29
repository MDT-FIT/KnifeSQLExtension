using Bogus;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generator;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generators;
using SqlParser.Ast;
using System.Collections.Immutable;
using static SqlParser.Ast.DataType;
using Microsoft.Extensions.Logging;


namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class GenerationService
    {
        private readonly IDatabaseClient _client;
        private readonly DependenciesService _dependenciesService;
        private readonly TableService _tableService;
        private readonly IGenerator? _generator;
        private readonly Faker _faker = new();
        private readonly ILogger<GenerationService> _logger;

        // Cached table data for faster computations of primary and unique constraint values
        private readonly Dictionary<string, List<Dictionary<string, object>>> _cachedTableData = [];

        public GenerationService(IDatabaseClient client, DependenciesService dependenciesService, TableService tableService, ILogger<GenerationService> logger)
        {
            _client = client;
            _dependenciesService = dependenciesService;
            _tableService = tableService;
            _logger = logger;

            Type generatorType = client.DatabaseType switch
            {
                Core.Services.Database.DatabaseType.MsSql => typeof(MsSqlGenerator),
                Core.Services.Database.DatabaseType.MySql => typeof(MySqlGenerator),
                Core.Services.Database.DatabaseType.PostgreSql => typeof(PostgreSqlGenerator),
                _ => typeof(MsSqlGenerator),
            };
            _generator = Activator.CreateInstance(generatorType) as IGenerator;
        }

        public async Task Generate(List<string> tables, int rowCount)
        {
            await CacheTables(tables);

            var schemas = await _tableService.GetTablesAsync();
            var sortedTables = await _dependenciesService.GetSortedTables();
            
            _logger.LogInformation("Starting data generation for tables: {Tables}", string.Join(", ", tables));

            // For each table (already topologically sorted)
            sortedTables.Reverse(); // Reverse to iterate from least dependent to most dependent
            foreach(var table in sortedTables)
            {
                _logger.LogInformation("Generating data for table: {Table}", table);
                var rows = new List<Dictionary<string, object>>();
                if(tables.Contains(table))
                {
                    var schema = schemas.First(s => s.FullName == table);

                    for(int i = 0; i < rowCount; i++)
                    {
                        var row = new Dictionary<string, object>();
                        await GenerateTableData(schema, row);
                        rows.Add(row);
                        _cachedTableData[table].Add(row);
                    }
                }
                await _tableService.SeedTable(table, rows);
                await CacheTables(tables);
            }

            // Clear cache
            _cachedTableData.Clear();
            _logger.LogInformation("Data generation completed successfully");
        }

        private async Task CacheTables(List<string> tables)
        {
            _cachedTableData.Clear();

            var allTables = tables
                .Concat(await _dependenciesService.GetDependenciesAsync(tables))
                .Distinct();

            foreach(var tableName in allTables)
                _cachedTableData[tableName] = await _tableService.GetTableDataAsync(tableName);
        }

        private async Task GenerateTableData(TableSchema schema, Dictionary<string, object> row)
        {
            await GenerateForeignConstraintValues(schema, row);
            GeneratePrimaryConstraintValues(schema, row);    // already skips row.ContainsKey internally
            GenerateUniqueConstraintValues(schema, row);     // already skips row.ContainsKey internally
            GeneratePlainValues(schema, row);
        }

        private void GeneratePlainValues(TableSchema schema, Dictionary<string, object> row)
        {
            foreach(var column in schema.Columns.Where(c => !row.ContainsKey(c.Name)))
            {
                if(column.IsIdentity || column.IsComputed)
                    continue;

                row[column.Name] = _generator.GenerateValue(_faker, column);
            }
        }

        private async Task GenerateForeignConstraintValues(TableSchema schema, Dictionary<string, object> row)
        {
            foreach(var constraint in schema.ForeignConstraints)
            {
                var refSchema = await _tableService.GetTableAsync(constraint.ReferencedTable);
                var refRows = _cachedTableData[constraint.ReferencedTable];

                ThrowExceptionIfNoData(refRows, $"No data in {refSchema.FullName}");
                var availableRows = refRows;

                var fkColumns = constraint.ColumnMappings
                    .Select(cm => cm.FromColumn)
                    .ToList();

                var fkColumnSet = fkColumns.ToHashSet();
                bool isUnique = IsPartOfUniqueConstraint(schema, fkColumnSet);

                if(isUnique)
                {
                    var refColumns = constraint.ColumnMappings
                        .Select(cm => cm.ToColumn)
                        .ToList();

                    availableRows = SelectUniqueRows(schema, refColumns, refRows, fkColumns, row);
                }

                ThrowExceptionIfNoData(availableRows, $"No available FK values in {refSchema.FullName}");

                var selectedRow = _faker.PickRandom(availableRows);
                // Debug — see what keys actually came back
                var actualKeys = string.Join(", ", selectedRow.Keys);
                var expectedKeys = string.Join(", ", constraint.ColumnMappings.Select(cm => cm.ToColumn));
                // log or throw $"Actual: {actualKeys} | Expected: {expectedKeys}"

                foreach(var (fromCol, toCol) in constraint.ColumnMappings)
                {
                    row[fromCol] = selectedRow[toCol];
                }
            }
        }

        private void GeneratePrimaryConstraintValues(TableSchema schema, Dictionary<string, object> row)
        {
            var pkColumns = schema.Columns.Where(c => c.IsPrimaryKey).ToList();

            // Identity — skip, DB handles it
            if(pkColumns.All(c => c.IsIdentity))
                return;

            var existingRows = _cachedTableData[schema.FullName];
            int maxAttempts = 1000;

            var freePkColumns = pkColumns.Where(c => !c.IsIdentity && !row.ContainsKey(c.Name)).ToList();

            // All PK columns already filled by FK generation — just verify uniqueness once
            if(freePkColumns.Count == 0)
            {
                bool unique = existingRows.All(existing =>
                    pkColumns.Any(c => !Equals(existing[c.Name], row[c.Name]))
                );
                if(!unique)
                    throw new Exception($"Could not generate unique PK for '{schema.FullName}': " +
                        $"all PK columns are FK-locked but the combination already exists.");
                return;
            }

            while(maxAttempts-- > 0)
            {
                foreach(var col in freePkColumns)
                    row[col.Name] = _generator.GenerateValue(_faker, col);

                bool tupleIsUnique = existingRows.All(existing =>
                    pkColumns.Any(c => !Equals(existing[c.Name], row[c.Name]))
                );

                if(tupleIsUnique) return;
            }

            throw new Exception(
                $"Could not generate unique PK for '{schema.FullName}' after maximum attempts.");
        }

        private void GenerateUniqueConstraintValues(TableSchema schema, Dictionary<string, object> row)
        {
            foreach(var constraint in schema.UniqueConstraints)
            {
                // Skip if all columns already filled (e.g. by FK generation)
                if(constraint.Columns.All(c => row.ContainsKey(c)))
                    continue;

                var existingRows = _cachedTableData[schema.FullName];
                int maxAttempts = 1000;

                while(maxAttempts-- > 0)
                {
                    foreach(var colName in constraint.Columns.Where(c => !row.ContainsKey(c)))
                    {
                        var col = schema.Columns.First(c => c.Name == colName);
                        row[colName] = _generator.GenerateValue(_faker, col);
                    }

                    bool tupleIsUnique = existingRows.All(existing =>
                        constraint.Columns.Any(c => !Equals(existing[c], row[c]))
                    );

                    if(tupleIsUnique) return;
                }

                throw new Exception(
                    $"Could not generate unique values for constraint '{constraint.Name}' " +
                    $"in '{schema.FullName}' after maximum attempts.");
            }
        }

        private List<Dictionary<string, object>> SelectUniqueRows(
            TableSchema schema,
            IEnumerable<string> refColumns,
            List<Dictionary<string, object>> refRows,
            List<string> fkColumns,
            Dictionary<string, object> currentRow)
        {
            var allRows = _cachedTableData[schema.FullName]
                .Where(r => fkColumns.All(c => r.ContainsKey(c)))
                .ToList();

            var usedTuples = allRows
                .Select(r => string.Join("|", fkColumns.Select(c => r[c])))
                .ToHashSet();

            return [.. refRows
                .Where(r =>
                {
                    var tuple = string.Join("|", refColumns.Select(c => r[c]));
                    return !usedTuples.Contains(tuple);
                })];
        }

        private static bool IsPartOfUniqueConstraint(TableSchema schema, HashSet<string> columnSet)
        {
            bool isPrimaryKey = schema.Columns
                                .Where(c => c.IsPrimaryKey)
                                .Select(c => c.Name)
                                .ToHashSet()
                                .SetEquals(columnSet);

            bool isUniqueConstraint = schema.UniqueConstraints
                .Any(uc => uc.Columns.ToHashSet().SetEquals(columnSet));

            bool isUnique = isPrimaryKey || isUniqueConstraint;
            return isUnique;
        }

        private static void ThrowExceptionIfNoData(ICollection<Dictionary<string, object>> collection, string message)
        {
            if(collection.Count == 0)
                throw new Exception(message);
        }
    }
}
