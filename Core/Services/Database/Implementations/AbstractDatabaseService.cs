using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System.Data;
using System.Data.Common;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Provides a base implementation for database interaction using the ADO.NET abstraction layer.
    // This abstract class implements common CRUD operations and connection management logic 
    // that are shared across different database providers (e.g., MS SQL, PostgreSQL).
    public abstract class AbstractDatabaseService : IDatabaseClient
    {
        // Gets the specific database provider type.
        public abstract Type Type { get; }

        // The underlying database connection object
        protected DbConnection _connection;

        // Creates a provider-specific instance of a database connection
        protected abstract DbConnection CreateConnection(string connectionString);

        // Asynchronously opens a database connection using the specified connection string,
        // returning true if successful, thrown exception when the connection attempt fails
        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _connection = CreateConnection(connectionString);
                await _connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка {Type}: {ex.Message}");
            }
        }

        // Asynchronously closes and disposes of the current database connection
        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        /// Common CRUD operations
        // Retrieves all data from the specified table
        // Return a list of dictionaries representing database rows and their values
        public async Task<List<Dictionary<string, object>>> GetDataAsync(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            return await ExecuteQueryAsync(query);
        }

        // Asynchronously inserts a single record into the specified table
        // Returns a dictionary containing column names as keys and their corresponding values
        public async Task InsertDataAsync(string tableName, Dictionary<string, object> data)
        {
            EnsureConnectionOpen();

            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = query;
                AddParameters(command, data);
                await command.ExecuteNonQueryAsync();
            }
        }

        // Asynchronously updates existing records based on a specific ID column inside specific table

        public async Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data)
        {
            EnsureConnectionOpen();

            var updates = new List<string>();
            foreach (var key in data.Keys)
            {
                updates.Add($"{key}=@{key}");
            }
            string updateString = string.Join(", ", updates);
            string query = $"UPDATE {tableName} SET {updateString} WHERE {idColumn} = @IdVal";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = query;
                AddParameters(command, data);

                var idParam = command.CreateParameter();
                idParam.ParameterName = "@IdVal";
                idParam.Value = idValue;
                command.Parameters.Add(idParam);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Asynchronously deletes a record from the database's table using a unique identifier
        public async Task DeleteDataAsync(string tableName, string idColumn, string idValue)
        {
            EnsureConnectionOpen();

            string query = $"DELETE FROM {tableName} WHERE {idColumn} = @IdVal";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = query;
                var idParam = command.CreateParameter();
                idParam.ParameterName = "@IdVal";
                idParam.Value = idValue;
                command.Parameters.Add(idParam);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Executes a raw SQL query asynchronously and returns the result set as a list of dictionaries
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();
            EnsureConnectionOpen();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    do
                    {
                        if (reader.FieldCount == 0)
                        {
                            results.Add(new Dictionary<string, object> { { "Rows Affected", reader.RecordsAffected } });
                        }
                        else
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                results.Add(row);
                            }
                        }
                    }
                    while (await reader.NextResultAsync());
                }
            }
            return results;
        }

        /// Methods for realization in specific classes

        // Retrieves a list of table names available in the current database.
        public abstract Task<List<string>> GetTablesAsync();

        // Retrieves detailed schema information for a specific table.
        public abstract Task<TableSchema> GetTableSchemaAsync(string tableName, string schema = "dbo");

        // Retrieves a list of all schemas defined in the database.
        public abstract Task<List<string>> GetDatabaseSchemasAsync();

        /// Additional methods of basic class
        // Verifies that the database connection is currently open.
        protected void EnsureConnectionOpen()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new Exception("Немає з'єднання!");
        }

        // Method to map dictionary values to database command parameters.
        protected void AddParameters(DbCommand command, Dictionary<string, object> data)
        {
            foreach (var item in data)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@" + item.Key;
                param.Value = item.Value ?? DBNull.Value;
                command.Parameters.Add(param);
            }
        }
    }
}
