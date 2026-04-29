using KnifeSQLExtension.Core.Services.Database.Implementations;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.Extensions.Logging;

namespace KnifeSQLExtension.Core.Services.Database;

public class DatabaseFactory
{
        public static IDatabaseClient CreateDatabaseClient(DatabaseType providerName, ILoggerFactory loggerFactory)
        {
            return providerName switch
            {
                DatabaseType.MsSql => new MsSqlDatabaseService(loggerFactory.CreateLogger<MsSqlDatabaseService>()),
                DatabaseType.PostgreSql => new PostgresDatabaseService(loggerFactory.CreateLogger<PostgresDatabaseService>()),
                _ => throw new NotSupportedException($"Provider '{providerName}' is not supported.")
            };
        }
}