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
        private List<TableSchema> _cachedTables = null!;

        public TableService(IDatabaseClient client)
        {
            _client = client;
        }

        public async Task<List<TableSchema>> GetTablesAsync(bool forceRefresh=false)
        {
            if(_cachedTables is not null && !forceRefresh)
                return _cachedTables;

            List<TableSchema> tableSchemas = [];
            var tableFullNames = await _client.GetTablesAsync();

            foreach(var table in tableFullNames)
            {
                tableSchemas.Add(await _client.GetTableSchemaAsync(table));
            }

            // Save tables to cache
            _cachedTables = tableSchemas;

            return tableSchemas;
        }

        public async Task<List<TableSchema>> GetTablesAsync(string schema, bool forceRefresh=false)
        {
            return [.. (await GetTablesAsync(forceRefresh)).Where(s => s.SchemaName == schema)];
        }

        public async Task<List<string>> GetDatabaseSchemaList()
        {
            return await _client.GetDatabaseSchemasAsync();
        }
    }
}
