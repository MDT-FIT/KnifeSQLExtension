using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Asynchronously retrieves the list of table schemas available from the connected data source.
        /// </summary>
        /// <remarks>Subsequent calls may return cached results unless <paramref name="forceRefresh"/> is
        /// set to <see langword="true"/>. This method is thread-safe.</remarks>
        /// <param name="forceRefresh">If set to <see langword="true"/>, forces the method to bypass any cached results and retrieve the latest
        /// table schemas from the data source. If <see langword="false"/>, cached results may be returned if available.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="TableSchema"/> objects describing each table. The list will be empty if no tables are found.</returns>
        public async Task<List<TableSchema>> GetTablesAsync(bool forceRefresh=false)
        {
            if(_cachedTables is not null && !forceRefresh)
                return _cachedTables;

            List<TableSchema> tableSchemas = [];
            var tableFullNames = await _client.GetTablesAsync();

            foreach(var table in tableFullNames)
            {
                var parts = table.Split('.');

                tableSchemas.Add(await _client.GetTableSchemaAsync(parts[1], parts[0]));
            }

            // Save tables to cache
            _cachedTables = tableSchemas;

            return tableSchemas;
        }

        /// <summary>
        /// Asynchronously retrieves the list of tables for the specified database schema.
        /// </summary>
        /// <param name="schema">The name of the database schema for which to retrieve tables. Cannot be null or empty.</param>
        /// <param name="forceRefresh">true to force a refresh of the table metadata from the data source; otherwise, false to use cached data if
        /// available.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of TableSchema objects
        /// for the specified schema. The list will be empty if no tables are found.</returns>
        public async Task<List<TableSchema>> GetTablesAsync(string schema, bool forceRefresh=false)
        {
            return [.. (await GetTablesAsync(forceRefresh)).Where(s => s.SchemaName == schema)];
        }

        /// <summary>
        /// Asynchronously retrieves a list of all available database schema names.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings, each
        /// representing the name of a database schema. The list is empty if no schemas are available.</returns>
        public async Task<List<string>> GetDatabaseSchemaListAsync()
        {
            return await _client.GetDatabaseSchemasAsync();
        }

        public async Task<List<Dictionary<string, object>>> GetTableDataAsync(string table)
        {
            return await _client.GetDataAsync(table);
        }

        public async Task SeedTable(string table, List<Dictionary<string, object>> rows)
        {
            foreach(var row in rows)
                await _client.InsertDataAsync(table, row);
        }

        public async Task<TableSchema> GetTableAsync(string table)
        {
            if(_cachedTables.IsNullOrEmpty())
                _cachedTables = await GetTablesAsync();

            return _cachedTables.First(t => t.FullName == table);
        }
    }
}
