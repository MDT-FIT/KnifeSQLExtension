using KnifeSQLExtension.Core.Services.Database;

namespace KnifeSQLExtension.Core.Services;

public class ConnectionStringParser
{
    /// <summary>
    /// Define what databse to use depending on connection string format
    /// Current implementation is very basic and relies on simple keyword checks.
    /// It can be extended later
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static DatabaseType ParseConnectionString(string connectionString)
    {

        string normalizedConnectionString = connectionString.ToLowerInvariant();

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