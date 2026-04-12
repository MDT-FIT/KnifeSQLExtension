using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Core.Models
{
    public sealed class ColumnSchema
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsComputed { get; set; }
        public bool IsUnique { get; set; }
        public bool HasDefault { get; set; }
        public bool IsPlain 
        { 
            get => !(IsPrimaryKey && IsUnique && FkObject is not null); 
        }
        public FkObject? FkObject { get; set; } = null;
    }
}
