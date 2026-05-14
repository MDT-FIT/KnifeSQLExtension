using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models
{
    /// <summary>
    /// Represents a snapshot of a table's data at a specific point in time.
    /// </summary>
    public class DbSnapshot
    {
        public string TableName { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; } = DateTime.Now;

        // Each row is a dictionary: column name -> value
        public List<Dictionary<string, object>> Rows { get; set; } = new();

        // Column names in order
        public List<string> Columns { get; set; } = new();
    }
}
