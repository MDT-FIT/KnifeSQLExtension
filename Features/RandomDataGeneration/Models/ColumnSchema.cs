using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Models
{
    public class ColumnSchema
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public int MaxLength { get; set; } 
        public bool IsNullable { get; set; }    
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsComputed { get; set; }
        public bool HashDefault { get; set; }
        public string FkTableName { get; set; } = string.Empty;
        public string FkColumnName { get; set; } = string.Empty;
    }
}
