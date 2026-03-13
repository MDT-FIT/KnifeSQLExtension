using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models
{
    public class TableSchema
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<ColumnSchema> Columns { get; set; } = [];

        public TableSchema(string name)
        {
            Name = name;
        }
    }
}
