using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models.Constraints
{
    public class UniqueConstraint : Constraint
    {
        public List<string> Columns { get; set; } = [];
    }
}
