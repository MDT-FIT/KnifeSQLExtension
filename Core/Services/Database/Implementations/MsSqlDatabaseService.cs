using KnifeSQLExtension.Core.Constants;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Implementation of IDatabaseClient for Microsoft SQL Server
    public class MsSqlDatabaseService : IDatabaseClient
    {
        // Storing an active connection instance to be used accros queries
        private SqlConnection _connection;

        // Method to establish connection to the DB
        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _connection = new SqlConnection(connectionString);
                await _connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка MS SQL: {ex.Message}");
            }
        }

        // Method to safely close and dispose the DB connection 
        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task<List<string>> GetTablesAsync()
        {
            // Queries the internal SQL Server schema to find all user-created tables
            // Now fetching both SCHEMA and TABLE NAME to prevent duplicates (e.g. dbo.Users)
            string query = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

            // Reusing universal execution method to get the data
            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.ContainsKey("TABLE_SCHEMA") && row.ContainsKey("TABLE_NAME"))
                {
                    // Format: SchemaName.TableName
                    tables.Add($"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}");
                }
            }
            return tables;
        }

        // READ (SELECT *)
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            // Simple select all query
            string query = $"SELECT * FROM {tableName}";

            // Reuses the universal method
            return await ExecuteQueryAsync(query);
        }

        // CREATE (INSERT)
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            // Ensure we are actually connected before trying to query
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Forming dynamic SQL: INSERT INTO Table (Col1, Col2) VALUES (@Col1, @Col2)
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            // Using block ensures the SqlCommand is disposed from memory after execution
            using (var command = new SqlCommand(query, _connection))
            {
                // Using parameterized queries (@Key) instead of directly injecting values into the string
                // to prevent SQL Injection attacks.
                foreach (var item in data)
                {
                    // Add values as parameters
                    // DBNull.Value handles C# nulls correctly for SQL
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }

                // ExecuteNonQueryAsync is used for operations
                // that do not return a table (Insert/Update/Delete)
                await command.ExecuteNonQueryAsync();
            }
        }

        // UPDATE
        public async Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Forming the list of updations by setting clauses dynamically
            // "Name = @Name, Age = @Age"
            var updates = new List<string>();
            foreach (var key in data.Keys)
            {
                updates.Add($"{key}=@{key}");
            }
            string updateString = string.Join(", ", updates);

            // Constructing final UPDATE query with inserting idColumn inside
            string query = $"UPDATE {tableName} SET {updateString} WHERE {idColumn} = @IdVal";

            using (var command = new SqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    // Add values of fields
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                // Add ID value with which we are seeking a raw
                command.Parameters.AddWithValue("@IdVal", idValue);

                await command.ExecuteNonQueryAsync();
            }
        }

        // DELETE
        public async Task DeleteDataAsync(string tableName, string idColumn, string idValue)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Establish idColumn into the raw and valuew via parameter
            string query = $"DELETE FROM {tableName} WHERE {idColumn} = @IdVal";

            using (var command = new SqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@IdVal", idValue);
                await command.ExecuteNonQueryAsync();
            }
        }

        // Universal method for ANY query execution
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає підключення до бази даних!");

            using (var command = new SqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                // Using a do...while loop with NextResultAsync() to handle multiple query statements
                // executed in a single batch (e.g., "UPDATE Users; SELECT * FROM Users;")
                do
                {
                    // If it is INSERT/UPDATE/DELETE - there are no columns for reading
                    // FieldCount == 0 - no tabular data is returned.
                    if (reader.FieldCount == 0)
                    {
                        var row = new Dictionary<string, object>();
                        // reader.RecordsAffected return quantity of affected rows
                        row.Add("Rows Affected", reader.RecordsAffected);
                        results.Add(row);
                    }
                    else
                    {
                        // It's a SELECT query
                        // Read row by row.
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            // Dynamically map column names to their values for the current row
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
                // Read all data till the end
                // Moves to the next result set if one exists
                while (await reader.NextResultAsync());
            }
            return results;
        }

        /// <summary>
        /// Retrieves the schema information for the specified table, including column definitions and related metadata.
        /// </summary>
        /// <remarks>The returned schema includes details such as column names, data types, nullability,
        /// primary key status, identity columns, computed columns, default values, and foreign key relationships. This
        /// method queries the database asynchronously and may throw an exception if the table does not exist or if the
        /// database connection fails.</remarks>
        /// <param name="tableName">The name of the table for which to retrieve schema information. Cannot be null.</param>
        /// <returns>A <see cref="TableSchema"/> object containing the column definitions and metadata for the specified table.</returns>
        public async Task<TableSchema> GetTableSchemaAsync(string tableName)
        {
            string query = MSSqlServerTableSchemaQuery.Query(tableName);

            var data = await ExecuteQueryAsync(query);

            var schema = new TableSchema(tableName);

            foreach (var row in data)
            {
                var colSchema = new ColumnSchema();

                // Parse column properties
                string? name = row[nameof(colSchema.Name)].ToString();
                string? type = row[nameof(colSchema.SqlType)].ToString();
                int maxLength = Convert.ToInt32(row[nameof(colSchema.MaxLength)].ToString());
                int isNullable = Convert.ToInt32(row[nameof(colSchema.IsNullable)].ToString());
                int isPrimaryKey = Convert.ToInt32(row[nameof(colSchema.IsPrimaryKey)].ToString());
                int isIdentity = Convert.ToInt32(row[nameof(colSchema.IsIdentity)].ToString());
                int isComputed = Convert.ToInt32(row[nameof(colSchema.IsComputed)].ToString());
                int hasDefault = Convert.ToInt32(row[nameof(colSchema.HasDefault)].ToString());
                var fkRaw = row["ForeignKeysJson"];
                string? jsonString = (fkRaw == DBNull.Value || fkRaw == null) ? null : fkRaw.ToString();

                ArgumentNullException.ThrowIfNull(name);
                ArgumentNullException.ThrowIfNull(type);

                colSchema.Name = name;
                colSchema.SqlType = type;
                colSchema.MaxLength = maxLength == 0 ? null : maxLength;
                colSchema.IsNullable = isNullable == 1;
                colSchema.IsPrimaryKey = isPrimaryKey == 1;
                colSchema.IsIdentity = isIdentity == 1;
                colSchema.IsComputed = isComputed == 1;
                colSchema.HasDefault = hasDefault == 1;
                colSchema.FkObject = string.IsNullOrWhiteSpace(jsonString) ? null : JsonSerializer.Deserialize<FkObject>(jsonString);

                schema.Columns.Add(colSchema);
            }

            return schema;
        }

        /// <summary>
        /// Retrieves a list of database schemas
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetDatabaseSchemasAsync()
        {
            string query = "SELECT name, schema_id FROM sys.schemas;";
            List<string> schemas = [];

            using(var command = new SqlCommand(query, _connection))
            {
                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        if(reader[0] is not null)
                            schemas.Add(reader[0]?.ToString() ?? string.Empty);
                    }
                }
            }

            return schemas;
        }
    }
}