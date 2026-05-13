namespace KnifeSQLExtension.Core.Constants.SchemaQueries
{
    internal static class MSSqlServerTableSchemaQuery
    {
        public static string Query(string table, string schema = "dbo")
        {
            return $@"
            SELECT
                c.COLUMN_NAME AS Name,
                c.DATA_TYPE AS SqlType,
                ISNULL(c.CHARACTER_MAXIMUM_LENGTH, 0) AS MaxLength,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable,

                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,

                CASE 
                    WHEN pk.COLUMN_NAME IS NOT NULL THEN 1
                    WHEN uq.COLUMN_NAME IS NOT NULL THEN 1
                    ELSE 0
                END AS IsUnique,

                COLUMNPROPERTY(
                    OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME),
                    c.COLUMN_NAME,
                    'IsIdentity'
                ) AS IsIdentity,

                COLUMNPROPERTY(
                    OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME),
                    c.COLUMN_NAME,
                    'IsComputed'
                ) AS IsComputed,

                CASE WHEN c.COLUMN_DEFAULT IS NOT NULL THEN 1 ELSE 0 END AS HasDefault,

                ISNULL(
                (
                    SELECT TOP 1
                        ccu.TABLE_SCHEMA + '.' + ccu.TABLE_NAME AS FkFullTableName,
                        ccu.COLUMN_NAME AS FkColumnName
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                        ON kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
                        AND kcu.TABLE_SCHEMA = c.TABLE_SCHEMA
                        AND kcu.TABLE_NAME = c.TABLE_NAME
                        AND kcu.COLUMN_NAME = c.COLUMN_NAME
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                        ON ccu.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                ),
                NULL
                ) AS ForeignKeysJson

            FROM INFORMATION_SCHEMA.COLUMNS c

            LEFT JOIN (
                SELECT 
                    kcu.TABLE_SCHEMA,
                    kcu.TABLE_NAME,
                    kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk
                ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
                AND pk.TABLE_NAME = c.TABLE_NAME
                AND pk.COLUMN_NAME = c.COLUMN_NAME

            LEFT JOIN (
                SELECT 
                    kcu.TABLE_SCHEMA,
                    kcu.TABLE_NAME,
                    kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'UNIQUE'
            ) uq
                ON uq.TABLE_SCHEMA = c.TABLE_SCHEMA
                AND uq.TABLE_NAME = c.TABLE_NAME
                AND uq.COLUMN_NAME = c.COLUMN_NAME

            WHERE c.TABLE_NAME = '{table}'
              AND c.TABLE_SCHEMA = '{schema}'

            ORDER BY c.ORDINAL_POSITION;";
        }
    }
}