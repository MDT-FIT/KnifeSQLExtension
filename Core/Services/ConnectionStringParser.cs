using KnifeSQLExtension.Core.Services.Database;

namespace KnifeSQLExtension.Core.Services;

public class ConnectionStringParser
{
    public static DatabaseType ParseConnectionString(string connectionString)
    {
        
        var normalizedConnectionString = connectionString.ToLowerInvariant();
        
        if (normalizedConnectionString.Contains("server=") && normalizedConnectionString.Contains("database="))
        {
            return DatabaseType.MsSql;
        }
        else if (normalizedConnectionString.Contains("postgres"))
        {
            return DatabaseType.PostgreSql;
        }
        else
        {
            throw new NotSupportedException("Unsupported connection string format.");
        }
    }
}