using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    public class SqlDatabaseService : IDatabaseClient
    {
        private SqlConnection _connection;

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
                throw new Exception($"Помилка SQL: {ex.Message}");
            }
        }

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
            // get list of tables
            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.ContainsKey("TABLE_NAME"))
                {
                    tables.Add(row["TABLE_NAME"].ToString());
                }
            }
            return tables;
        }

        // READ (SELECT *)
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            // via universal method
            string query = $"SELECT * FROM {tableName}";
            return await ExecuteQueryAsync(query);
        }

        // CREATE (INSERT)
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // forming dynamic SQL: INSERT INTO Table (Col1, Col2) VALUES (@Col1, @Col2)
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = new SqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    // add values as parameters
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
            }
        }

        // UPDATE
        public async Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // forming the list of updations
            var updates = new List<string>();
            foreach (var key in data.Keys)
            {
                updates.Add($"{key}=@{key}");
            }
            string updateString = string.Join(", ", updates);

            // inserting idColumn inside the query
            string query = $"UPDATE {tableName} SET {updateString} WHERE {idColumn} = @IdVal";

            using (var command = new SqlCommand(query, _connection))
            {
                foreach (var item in data)
                {
                    // add values of fields
                    command.Parameters.AddWithValue("@" + item.Key, item.Value ?? DBNull.Value);
                }
                // add ID value with which we are seeking a raw
                command.Parameters.AddWithValue("@IdVal", idValue);

                await command.ExecuteNonQueryAsync();
            }
        }

        // DELETE
        public async Task DeleteDataAsync(string tableName, string idColumn, string idValue)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає з'єднання!");

            // establish idColumn into the raw and valuew via parameter
            string query = $"DELETE FROM {tableName} WHERE {idColumn} = @IdVal";

            using (var command = new SqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@IdVal", idValue);
                await command.ExecuteNonQueryAsync();
            }
        }
        // universal method for query execution
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                throw new Exception("Немає підключення до бази даних!");

            using (var command = new SqlCommand(query, _connection))
            using (var reader = await command.ExecuteReaderAsync())
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
            return results;
        }
    }
}