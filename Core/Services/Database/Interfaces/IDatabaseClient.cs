

namespace KnifeSQLExtension.Core.Services.Database.Interfaces
{
    public interface IDatabaseClient
    {
        // Connection
        Task<bool> ConnectAsync(string connectionString);
        Task DisconnectAsync();

        // get list of tables
        Task<List<string>> GetTablesAsync();

        // universal query
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query);

        //CRUD  

        // READ 
        Task<List<Dictionary<string, object>>> GetDataAsync(string tableName);

        // CREATE 
        Task InsertDataAsync(string tableName, Dictionary<string, object> data);

        // UPDATE 
        Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data);

        // DELETE 
        Task DeleteDataAsync(string tableName, string idColumn, string idValue);
    }
}