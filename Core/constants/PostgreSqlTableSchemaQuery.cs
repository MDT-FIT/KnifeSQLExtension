using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Constants
{
    internal static class PostgreSqlTableSchemaQuery
    {
        public static string Query(string tableName)
        {
            return $@"SELECT
                    c.column_name AS Name,
                    c.udt_name AS SqlType,
                    c.character_maximum_length AS MaxLength,
                    CASE WHEN c.is_nullable = 'YES' THEN TRUE ELSE FALSE END AS IsNullable,
                    CASE WHEN pk.column_name IS NOT NULL THEN TRUE ELSE FALSE END AS IsPrimaryKey,
                    CASE WHEN c.column_default LIKE 'nextval%' THEN TRUE ELSE FALSE END AS IsIdentity,
                    (pgc.attgenerated = 's') AS IsComputed,
                    FALSE AS HasDefault, -- placeholder
                    COALESCE(fk.fk_json, '[]') AS ForeignKeysJson
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT
                        kcu.column_name,
                        kcu.table_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON kcu.constraint_name = tc.constraint_name
                       AND kcu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                      AND tc.table_name = '{tableName}'
                ) pk
                    ON pk.column_name = c.column_name
                   AND pk.table_name = c.table_name
                LEFT JOIN LATERAL (
                    SELECT json_agg(
                        json_build_object(
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
                ) fk ON TRUE
                JOIN pg_catalog.pg_attribute pgc
                    ON pgc.attname = c.column_name
                   AND pgc.attrelid = ('""' || c.table_schema || '"".""' || c.table_name || '""')::regclass
                WHERE c.table_name = '{tableName}'
                ORDER BY c.ordinal_position;";
        }
    }
}
