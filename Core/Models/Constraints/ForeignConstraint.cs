using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models.Constraints
{
    public sealed class ForeignConstraint : Constraint
    {
        public string ReferencedTable { get; set; }

        public List<(string FromColumn, string ToColumn)> ColumnMappings { get; set; } = [];
    }
}
