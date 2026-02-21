

namespace KnifeSQLExtension.Core.Services.Database.Interfaces
{
    // Interface that define a standard behaviout(methods) for
    // cooncrete classes of DB handling
    public interface IDatabaseClient
    {
        // Connection managment
        Task<bool> ConnectAsync(string connectionString);
        Task DisconnectAsync();

        // get list of available tables in the connected DB
        Task<List<string>> GetTablesAsync();

        // universal query for execution of any SQL query
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query);

        //CRUD operations

        // READ - fetch all data from specific table
        Task<List<Dictionary<string, object>>> GetDataAsync(string tableName);

        // CREATE - insert a new row into table. New data contains 
        // columns names and their values
        Task InsertDataAsync(string tableName, Dictionary<string, object> data);

        // UPDATE - modify an existing row based on a specific ID
        Task UpdateDataAsync(string tableName, string idColumn, string idValue, Dictionary<string, object> data);

        // DELETE - remove a row based on a specific ID
        Task DeleteDataAsync(string tableName, string idColumn, string idValue);
    }
}