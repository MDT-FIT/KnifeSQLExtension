internal static class PostgreSqlTableSchemaQuery
{
    public static string Query(string table, string schema)
    {
        return $@"
            SELECT
                c.column_name AS Name,
                c.udt_name AS SqlType,
                c.character_maximum_length AS MaxLength,
                (c.is_nullable = 'YES') AS IsNullable,

                (pk.column_name IS NOT NULL) AS IsPrimaryKey,

                (c.column_default LIKE 'nextval%') AS IsIdentity,

                (pgc.attgenerated = 's') AS IsComputed,

                (c.column_default IS NOT NULL) AS HasDefault,

                COALESCE(fk.fk_json, '[]') AS ForeignKeysJson

            FROM information_schema.columns c

            LEFT JOIN (
                SELECT
                    kcu.column_name,
                    kcu.table_name,
                    kcu.table_schema
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON kcu.constraint_name = tc.constraint_name
                   AND kcu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema = '{schema}'
                  AND tc.table_name = '{table}'
            ) pk
                ON pk.column_name = c.column_name
               AND pk.table_name = c.table_name
               AND pk.table_schema = c.table_schema

            LEFT JOIN LATERAL (
                SELECT json_agg(
                    json_build_object(
                        'FkFullTableName', ccu.table_schema || '.' || ccu.table_name,
                        'FkSchemaName', ccu.table_schema,
                        'FkTableName', ccu.table_name,
                        'FkColumnName', ccu.column_name
                    )
                ) AS fk_json
                FROM information_schema.table_constraints tc2
                JOIN information_schema.key_column_usage kcu2
                    ON kcu2.constraint_name = tc2.constraint_name
                   AND kcu2.table_schema = tc2.table_schema
                JOIN information_schema.constraint_column_usage ccu
                    ON ccu.constraint_name = tc2.constraint_name
                   AND ccu.table_schema = tc2.table_schema
                WHERE tc2.constraint_type = 'FOREIGN KEY'
                  AND kcu2.column_name = c.column_name
                  AND kcu2.table_name = c.table_name
                  AND kcu2.table_schema = c.table_schema
            ) fk ON TRUE

            JOIN pg_catalog.pg_attribute pgc
                ON pgc.attname = c.column_name
               AND pgc.attrelid = (quote_ident(c.table_schema) || '.' || quote_ident(c.table_name))::regclass

            WHERE c.table_name = '{table}'
              AND c.table_schema = '{schema}'

            ORDER BY c.ordinal_position;";
    }
}