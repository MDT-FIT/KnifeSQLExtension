using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models
{
    public sealed class ForeignConstraint
    {
        public string ConstraintName { get; set; }
        public string ReferencedTable { get; set; }

        public List<(string FromColumn, string ToColumn)> ColumnMappings { get; set; } = [];
    }
}
