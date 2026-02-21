using KnifeSQLExtension.Core.Services.Database.Interfaces;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Implementation of IDatabaseClient specifically for MySQL databases
    public class MySqlDatabaseService : IDatabaseClient
    {
        // Connection instance for the session
        private MySqlConnection _connection;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                // Initialize MySqlConnection with the provided string
                _connection = new MySqlConnection(connectionString);

                // Open connection asynchronously
                await _connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка MySQL: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            // Safely close and dispose MySQL connection
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task<List<string>> GetTablesAsync()
        {
            // Execute MySQL specific command to get all tables in the database
            string query = "SHOW TABLES";
            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.Values.Count > 0)
                {
                    tables.Add(row.Values.First().ToString());
                }
            }
            return tables;
        }

        // READ operation
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            return await ExecuteQueryAsync(query);
        }

        // CREATE operation
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // Dynamically generate column names and parameters
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = new MySqlCommand(query, _connection))
            {
                // Adding values as SQL parameters prevents SQL injection
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

            // Build SET statements dynamically storing in the list
            var updates = new List<string>();
            foreach (var key in data.Keys)
            {
                updates.Add($"{key}=@{key}");
            }
            string updateString = string.Join(", ", updates);

            string query = $"UPDATE {tableName} SET {updateString} WHERE {idColumn} = @IdVal";

            using (var command = new MySqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                // Bind target ID for WHERE clause
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

            using (var command = new MySqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@IdVal", idValue);
                await command.ExecuteNonQueryAsync();
            }
        }

        // Universal method to execute raw queries against MySQL
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            using (var command = new MySqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                // Check if query is non-tabular (INSERT/UPDATE/DELETE)
                if (reader.FieldCount == 0)
                {
                    var row = new Dictionary<string, object>();
                    row.Add("Rows Affected", reader.RecordsAffected);
                    results.Add(row);
                    return results;
                }

                // Parse rows dynamically
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