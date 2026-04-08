using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Constants.ForeignKeyQueries
{
    public static class MSSqlServerForeignKeysQuery
    {
        public static string Query(string table, string schema = "dbo")
        {
            return $@"
                SELECT
                    fk.name AS ConstraintName,
                    sch_ref.name + '.' + t_ref.name AS ReferencedTable,

                    c_parent.name AS FromColumn,
                    c_ref.name AS ToColumn

                FROM sys.foreign_keys fk

                INNER JOIN sys.foreign_key_columns fkc
                    ON fkc.constraint_object_id = fk.object_id

                INNER JOIN sys.tables t_parent
                    ON t_parent.object_id = fk.parent_object_id

                INNER JOIN sys.schemas sch_parent
                    ON sch_parent.schema_id = t_parent.schema_id

                INNER JOIN sys.columns c_parent
                    ON c_parent.object_id = t_parent.object_id
                   AND c_parent.column_id = fkc.parent_column_id

                INNER JOIN sys.tables t_ref
                    ON t_ref.object_id = fk.referenced_object_id

                INNER JOIN sys.schemas sch_ref
                    ON sch_ref.schema_id = t_ref.schema_id

                INNER JOIN sys.columns c_ref
                    ON c_ref.object_id = t_ref.object_id
                   AND c_ref.column_id = fkc.referenced_column_id

                WHERE t_parent.name = '{table}'
                  AND sch_parent.name = '{schema}'

                ORDER BY fk.name, fkc.constraint_column_id;";
        }
    }
}
