using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class TableService
    {
        private readonly IDatabaseClient _client;

        public TableService(IDatabaseClient client)
        {
            _client = client;
        }

        public async Task<List<TableSchema>> GetTablesAsync()
        {
            List<TableSchema> tableSchemas = [];
            var tableFullNames = await _client.GetTablesAsync();

            foreach(var table in tableFullNames)
            {
                tableSchemas.Add(await _client.GetTableSchemaAsync(table));
            }

            return tableSchemas;
        }

        public async Task<List<TableSchema>> GetTablesAsync(string schema)
        {
            return [.. (await GetTablesAsync()).Where(s => s.SchemaName == schema)];
        }

        public async Task<List<string>> GetDatabaseSchemaList()
        {
            return await _client.GetDatabaseSchemasAsync();
        }
    }
}
