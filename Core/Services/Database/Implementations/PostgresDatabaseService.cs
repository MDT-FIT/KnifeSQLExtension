using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Models.Constraints;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System.Text.Json;
using KnifeSQLExtension.Core.Constants.SchemaQueries;
using Npgsql;
using System.Data;
using OneOf.Types;
using Microsoft.Extensions.Logging;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Implementation of IDatabaseClient for PostgreSQL
    public class PostgresDatabaseService : IDatabaseClient
    {
        public DatabaseType DatabaseType { get; } = DatabaseType.PostgreSql;
        
        private NpgsqlConnection? _connection;
        private readonly ILogger<PostgresDatabaseService> _logger;
        
        public PostgresDatabaseService(ILogger<PostgresDatabaseService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _connection = new NpgsqlConnection(connectionString);
                await _connection.OpenAsync();
                _logger.LogInformation("Successfully connected to PostgresSQL database");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PostgresSQL database");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection is not null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
                await _connection.DisposeAsync();
            }
        }

        public async Task<List<string>> GetTablesAsync()
        {
            // Query PostgreSQL information_schema for user-created tables
            // Excludes system schemas (pg_* and information_schema)
            string query = @"
                SELECT table_schema, table_name 
                FROM information_schema.tables 
                WHERE table_type = 'BASE TABLE' 
                AND table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY table_schema, table_name";

            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.ContainsKey("table_schema") && row.ContainsKey("table_name"))
                {
                    // Format: schema.tablename
                    tables.Add($"{row["table_schema"]}.{row["table_name"]}");
                }
            }

            return tables;
        }

        // READ
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            return await ExecuteQueryAsync(query);
        }

        // CREATE
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new Exception("No connection!");
            
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    command.Parameters.AddWithValue("@" + item.Key, item.Value is null ? DBNull.Value : item.Value);
                }
                
                await command.ExecuteNonQueryAsync();
            }
        }

        // UPDATE
        public async Task UpdateDataAsync(string tableName, string idColumn, string idValue,
            Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new Exception("No connection!");
            
            var updates = new List<string>();
            foreach (var key in data.Keys)
            {
                updates.Add($"{key}=@{key}");
            }

            string updateString = string.Join(", ", updates);
            string query = $"UPDATE {tableName} SET {updateString} WHERE {idColumn} = @IdVal";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    command.Parameters.AddWithValue("@" + item.Key, item.Value is null ? DBNull.Value : item.Value);
                }
                command.Parameters.AddWithValue("@IdVal", idValue);

                await command.ExecuteNonQueryAsync();
            }
        }

        // DELETE
        public async Task DeleteDataAsync(string tableName, string idColumn, string idValue)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new Exception("No connection!");

            string query = $"DELETE FROM {tableName} WHERE {idColumn} = @IdVal";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@IdVal", idValue);
                await command.ExecuteNonQueryAsync();
            }
        }

        
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new Exception("Немає підключення до бази даних!");

            using (var command = new NpgsqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                do
                {
                    if (reader.FieldCount == 0)
                    {
                        var row = new Dictionary<string, object>();
                        row.Add("Rows Affected", reader.RecordsAffected);
                        results.Add(row);
                    }
                    else
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = reader.GetValue(i);
                                row.Add(columnName, value);
                            }

                            results.Add(row);
                        }
                    }
                }

                while (await reader.NextResultAsync());
            }

            return results;
        }
        
        public async Task<TableSchema> GetTableSchemaAsync(string tableName, string schema = "public")
        {
            string query = PostgreSqlTableSchemaQuery.Query(tableName, schema);
            var data = await ExecuteQueryAsync(query);
            var tableSchema = new TableSchema(tableName, schema);

            foreach (var row in data)
            {
                var colSchema = new ColumnSchema();

                // Parse column properties
                string? name = row["name"].ToString();
                string? type = row["sqltype"].ToString();
                
                // Handle MaxLength - PostgresSQL returns null for unlimited varchar
                int? maxLength = null;
                if (row.ContainsKey("maxlength") && row["maxlength"] is not null and not DBNull)
                {
                    if (int.TryParse(row["maxlength"].ToString(), out int length))
                        maxLength = length > 0 ? length : null;
                }

                // PostgresSQL returns boolean directly
                bool isNullable = false;
                if (row.ContainsKey("isnullable"))
                {
                    if (row["isnullable"] is bool boolVal)
                        isNullable = boolVal;
                    else if (bool.TryParse(row["isnullable"].ToString(), out bool parsed))
                        isNullable = parsed;
                }

                bool isPrimaryKey = false;
                if (row.ContainsKey("isprimarykey"))
                {
                    if (row["isprimarykey"] is bool boolVal)
                        isPrimaryKey = boolVal;
                    else if (bool.TryParse(row["isprimarykey"].ToString(), out bool parsed))
                        isPrimaryKey = parsed;
                }

                bool isIdentity = false;
                if (row.ContainsKey("isidentity"))
                {
                    if (row["isidentity"] is bool boolVal)
                        isIdentity = boolVal;
                    else if (bool.TryParse(row["isidentity"].ToString(), out bool parsed))
                        isIdentity = parsed;
                }

                bool isComputed = false;
                if (row.ContainsKey("iscomputed"))
                {
                    if (row["iscomputed"] is bool boolVal)
                        isComputed = boolVal;
                    else if (bool.TryParse(row["iscomputed"].ToString(), out bool parsed))
                        isComputed = parsed;
                }

                bool isUnique = false;
                if (row.ContainsKey("isunique"))
                {
                    if (row["isunique"] is bool boolVal)
                        isUnique = boolVal;
                    else if (bool.TryParse(row["isunique"].ToString(), out bool parsed))
                        isUnique = parsed;
                }

                bool hasDefault = false;
                if (row.ContainsKey("hasdefault"))
                {
                    if (row["hasdefault"] is bool boolVal)
                        hasDefault = boolVal;
                    else if (bool.TryParse(row["hasdefault"].ToString(), out bool parsed))
                        hasDefault = parsed;
                }

                var fkRaw = row.ContainsKey("foreignkeysjson") ? row["foreignkeysjson"] : null;
                string? jsonString = (fkRaw == DBNull.Value || fkRaw == null) ? null : fkRaw.ToString();

                ArgumentNullException.ThrowIfNull(name);
                ArgumentNullException.ThrowIfNull(type);

                colSchema.Name = name;
                colSchema.SqlType = type;
                colSchema.MaxLength = maxLength;
                colSchema.IsNullable = isNullable;
                colSchema.IsPrimaryKey = isPrimaryKey;
                colSchema.IsIdentity = isIdentity;
                colSchema.IsComputed = isComputed;
                colSchema.IsUnique = isUnique;
                colSchema.HasDefault = hasDefault;
                colSchema.FkObject = string.IsNullOrWhiteSpace(jsonString) || jsonString == "[]"
                    ? null
                    : JsonSerializer.Deserialize<List<FkObject>>(jsonString)?.FirstOrDefault();

                tableSchema.Columns.Add(colSchema);

                // Add primary keys separately to handle compound ones
                if (colSchema.IsPrimaryKey)
                    tableSchema.PrimaryKeyColumns.Add(colSchema.Name);
            }

            tableSchema.ForeignConstraints = await GetTableForeignConstraintsAsync(
                tableSchema.TableName, tableSchema.SchemaName);

            tableSchema.UniqueConstraints = await GetTableUniqueConstraintAsync(
                tableSchema.TableName, tableSchema.SchemaName);

            return tableSchema;
        }

        private static List<T> BuildConstraints<T>(IEnumerable<Dictionary<string, object>> data,
            Func<string, Dictionary<string, object>, T> factory,
            Action<T, Dictionary<string, object>> aggregator) where T : Models.Constraints.Constraint
        {
            var lookup = new Dictionary<string, T>();

            foreach (var row in data)
            {
                string key = row["constraintname"]?.ToString() ?? string.Empty;

                if (!lookup.TryGetValue(key, out var item))
                {
                    item = factory(key, row);
                    lookup[key] = item;
                }

                aggregator(item, row);
            }

            return [.. lookup.Values];
        }

        private async Task<List<ForeignConstraint>> GetTableForeignConstraintsAsync(string table, string schema = "public")
        {
            // PostgreSQL foreign keys query
            string query = $@"
                SELECT
                    tc.constraint_name AS ConstraintName,
                    ccu.table_schema || '.' || ccu.table_name AS ReferencedTable,
                    kcu.column_name AS FromColumn,
                    ccu.column_name AS ToColumn
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                    ON ccu.constraint_name = tc.constraint_name
                    AND ccu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                    AND kcu.table_name = @tableName
                    AND kcu.table_schema = @schema
                ORDER BY tc.constraint_name";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@tableName", table);
                command.Parameters.AddWithValue("@schema", schema);

                var data = new List<Dictionary<string, object>>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        data.Add(row);
                    }
                }

                return BuildConstraints(
                    data,
                    (name, row) => new ForeignConstraint
                    {
                        Name = name,
                        ReferencedTable = (row["referencedtable"]?.ToString()) ?? string.Empty
                    },
                    (fk, row) =>
                    {
                        string from = (row["fromcolumn"]?.ToString()) ?? string.Empty;
                        string to = (row["tocolumn"]?.ToString()) ?? string.Empty;
                        fk.ColumnMappings.Add((from, to));
                    });
            }
        }

        private async Task<List<Models.Constraints.UniqueConstraint>> GetTableUniqueConstraintAsync(string table, string schema = "public")
        {
            // PostgreSQL unique constraints query
            string query = $@"
                SELECT
                    tc.constraint_name AS ConstraintName,
                    kcu.column_name AS ColumnName
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'UNIQUE'
                    AND kcu.table_name = @tableName
                    AND kcu.table_schema = @schema
                ORDER BY tc.constraint_name, kcu.ordinal_position";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@tableName", table);
                command.Parameters.AddWithValue("@schema", schema);

                var data = new List<Dictionary<string, object>>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        data.Add(row);
                    }
                }

                return BuildConstraints(
                    data,
                    (name, _) => new Models.Constraints.UniqueConstraint
                    {
                        Name = name
                    },
                    (uc, row) =>
                    {
                        string column = (row["columnname"]?.ToString()) ?? string.Empty;
                        uc.Columns.Add(column);
                    });
            }
        }

        /// <summary>
        /// Retrieves a list of database schemas
        /// </summary>
        public async Task<List<string>> GetDatabaseSchemasAsync()
        {
            // PostgreSQL schemas query - excludes system schemas
            string query = @"
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast', 'pg_temp_1')
                ORDER BY schema_name";

            List<string> schemas = [];

            using (var command = new NpgsqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    if (reader[0] is not null)
                        schemas.Add((reader[0]?.ToString()) ?? string.Empty);
                }
            }

            return schemas;
        }
    }
}

