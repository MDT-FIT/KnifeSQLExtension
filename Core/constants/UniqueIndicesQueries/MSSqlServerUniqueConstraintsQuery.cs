using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Constants.UniqueIndicesQueries
{
    internal static class MSSqlServerUniqueConstraintsQuery
    {
        public static string Query(string table, string schema = "dbo")
        {
            return $@"
                SELECT
                    i.name AS ConstraintName,
                    c.name AS ColumnName

                FROM sys.indexes i

                INNER JOIN sys.tables t
                    ON t.object_id = i.object_id

                INNER JOIN sys.schemas sch
                    ON sch.schema_id = t.schema_id

                INNER JOIN sys.index_columns ic
                    ON ic.object_id = i.object_id
                   AND ic.index_id = i.index_id

                INNER JOIN sys.columns c
                    ON c.object_id = t.object_id
                   AND c.column_id = ic.column_id

                WHERE t.name = '{table}'
                  AND sch.name = '{schema}'
                  AND i.is_unique = 1
                  AND i.is_primary_key = 0

                ORDER BY i.name;";
        }
    }
}
