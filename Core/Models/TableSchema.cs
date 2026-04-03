using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models
{
    public class TableSchema
    {
        public string TableName { get; set; } = string.Empty;

        public string SchemaName { get; set; } = string.Empty;

        public string FullName
        {
            get
            {
                return $"{SchemaName}.{TableName}";
            }
        }

        public ICollection<ColumnSchema> Columns { get; set; } = [];

        public TableSchema(string fullName)
        {
            var parts = fullName.Split('.');

            SchemaName = parts[0];
            TableName = parts[1];
        }

        public TableSchema(string tableName, string schemaName)
        {
            TableName = tableName;
            SchemaName = schemaName;
        }
    }
}
