using Bogus;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generator;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class GenerationService
    {
        private readonly IDatabaseClient _client;
        private IGenerator? _generator;

        public GenerationService(IDatabaseClient client)
        {
            _client = client;
            Type generatorType;

            switch(client.Type)
            {
                case Core.Services.Database.Type.MsSql:
                    generatorType = typeof(MsSqlGenerator);
                    break;
                case Core.Services.Database.Type.MySql:
                    generatorType = typeof(MySqlGenerator);
                    break;
                case Core.Services.Database.Type.PostgreSql:
                    generatorType = typeof(PostgreSqlGenerator);
                    break;
                default:
                    generatorType = typeof(MsSqlGenerator);
                    break;
            }

            _generator = Activator.CreateInstance(generatorType) as IGenerator;
        }

        //private void Generate(List<TableSchema> schemas, List<string> sortedTables)
        //{
        //    var faker = new Faker();
        //    var generatedKeys = new Dictionary<string, List<object>>();

        //    // For each table (already topologically sorted)
        //    foreach(var table in sortedTables)
        //    {
        //        var schema = schemas.First(s => s.FullName == table);
        //        var rows = new List<Dictionary<string, object>>();

        //        for(int i = 0; i < rowCount; i++)
        //        {
        //            var row = new Dictionary<string, object>();

        //            foreach(var column in schema.Columns)
        //            {
        //                // Skip auto-managed columns
        //                if(column.IsIdentity || column.IsComputed)
        //                    continue;

        //                // FK column — sample from already generated keys
        //                if(column.FkObject is not null)
        //                {
        //                    var pool = generatedKeys.GetValueOrDefault(column.FkObject.FkTableName);
        //                    row[column.Name] = (pool is null || pool.Count == 0)
        //                        ? null
        //                        : faker.PickRandom(pool);
        //                    continue;
        //                }

        //                // Regular column — generate based on type/name
        //                row[column.Name] = _generator.GenerateValue(faker, column);
        //            }

        //            rows.Add(row);

        //            // Collect PK for child tables to reference later
        //            if(row.TryGetValue(schema.PrimaryKeyColumn, out var pk))
        //            {
        //                generatedKeys.TryAdd(table, []);
        //                generatedKeys[table].Add(pk);
        //            }
        //        }
        //    }
        //}
    }
}
