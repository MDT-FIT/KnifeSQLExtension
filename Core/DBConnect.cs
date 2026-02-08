using System;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using KnifeSQLExtension.Core.Services.Database.Implementations;

namespace KnifeSQLExtension.Core
{
    public enum DatabaseType
    {
        SqlServer,
        // TODO: Other DB 
    }

    public static class DbConnect
    {
        public static IDatabaseClient GetClient(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.SqlServer:
                    // return SQL realization
                    return new SqlDatabaseService();

                // case DatabaseType.MongoDb:
                //    return new MongoDatabaseService();

                default:
                    throw new NotImplementedException("Ця база ще не підтримується");
            }
        }
    }
}