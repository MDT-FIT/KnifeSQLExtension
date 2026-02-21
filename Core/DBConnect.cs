using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.Core.Services.Database.Implementations;

namespace KnifeSQLExtension.Core
{
    // Define the list of supported DB systems as enumerators
    public enum DatabaseType
    {
        SqlServer,
        PostgreSql,
        MySql
    }

    // Factory class that is responsible for providing 
    // correct database client instance
    public static class DbConnect
    {
        // Based on the selected DatabaseType this method returns 
        // specified implementation of IDatabaseClient interface
        public static IDatabaseClient GetClient(DatabaseType type)
        {
            return type switch
            {
                DatabaseType.SqlServer => new SqlDatabaseService(),
                DatabaseType.PostgreSql => new PostgresDatabaseService(), 
                DatabaseType.MySql => new MySqlDatabaseService(),  
                
                // if DB engine isn't implemented yet - throw an exception's message
                _ => throw new NotImplementedException("Ця база ще не підтримується")
            };
        }
    }
}