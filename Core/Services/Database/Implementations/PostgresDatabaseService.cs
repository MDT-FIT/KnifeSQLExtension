using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Npgsql;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Implementation of IDatabaseClient specifically for PostgreSQL databases.
    public class PostgresDatabaseService : IDatabaseClient
    {
        // Connection instance for the session
        private NpgsqlConnection _connection;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                // Initialize NpgsqlConnection with the provided string
                _connection = new NpgsqlConnection(connectionString);

                // Open connection asynchronously
                await _connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка PostgreSQL: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            // Close and free PostgreSQL connection resources safely
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task<List<string>> GetTablesAsync()
        {
            // Get list of tables specifically in the 'public' schema of Postgres
            string query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.ContainsKey("table_name"))
                {
                    tables.Add(row["table_name"].ToString());
                }
            }
            return tables;
        }

        // READ operation (SELECT *)
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            return await ExecuteQueryAsync(query);
        }

        // CREATE operation (INSERT)
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Dynamically build the INSERT query structure
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                // Use parameters (@Key) to protect against SQL Injection
                foreach (var item in data)
                {
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
            }
        }

        // UPDATE operation
        public async Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Build SET clauses dynamically storing them in the list
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
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                // Bind the ID value to pinpoint the specific record
                command.Parameters.AddWithValue("@IdVal", idValue);

                await command.ExecuteNonQueryAsync();
            }
        }

        // DELETE operation
        public async Task DeleteDataAsync(string tableName, string idColumn, string idValue)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            string query = $"DELETE FROM {tableName} WHERE {idColumn} = @IdVal";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@IdVal", idValue);
                await command.ExecuteNonQueryAsync();
            }
        }

        // Universal method for ANY query execution inside PostgreSQL
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            using (var command = new NpgsqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (reader.FieldCount == 0)
                {
                    var row = new Dictionary<string, object>();
                    row.Add("Rows Affected", reader.RecordsAffected);
                    results.Add(row);
                    return results;
                }

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
            return results;
        }
    }
}