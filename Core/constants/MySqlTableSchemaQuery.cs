using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Constants
{
    internal static class MySqlTableSchemaQuery
    {
        public static string Query(string databaseName, string tableName)
        {
            return $@"SELECT
                    c.COLUMN_NAME AS Name,
                    c.COLUMN_TYPE AS SqlType,
                    c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                    (c.IS_NULLABLE = 'YES') AS IsNullable,
                    (pk.COLUMN_NAME IS NOT NULL) AS IsPrimaryKey,
                    (c.EXTRA LIKE '%auto_increment%') AS IsIdentity,
                    FALSE AS IsComputed,
                    FALSE AS HasDefault,
                    IFNULL(
                        (SELECT JSON_ARRAYAGG(
                                    JSON_OBJECT(
                                        'FkTableName', kcu.REFERENCED_TABLE_NAME,
                                        'FkColumnName', kcu.REFERENCED_COLUMN_NAME
                                    )
                                )
                         FROM information_schema.KEY_COLUMN_USAGE kcu
                         WHERE kcu.TABLE_SCHEMA = c.TABLE_SCHEMA
                           AND kcu.TABLE_NAME = c.TABLE_NAME
                           AND kcu.COLUMN_NAME = c.COLUMN_NAME
                           AND kcu.REFERENCED_TABLE_NAME IS NOT NULL),
                        JSON_ARRAY()
                    ) AS ForeignKeysJson
                FROM information_schema.COLUMNS c
                LEFT JOIN (
                    SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
                    FROM information_schema.TABLE_CONSTRAINTS tc
                    JOIN information_schema.KEY_COLUMN_USAGE kcu
                        ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                       AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                      AND tc.TABLE_NAME = '{tableName}'
                ) pk
                    ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
                   AND pk.TABLE_NAME = c.TABLE_NAME
                   AND pk.COLUMN_NAME = c.COLUMN_NAME
                WHERE c.TABLE_NAME = '{tableName}'
                  AND c.TABLE_SCHEMA = '{databaseName}'
                ORDER BY c.ORDINAL_POSITION;";
        }
    }
}
