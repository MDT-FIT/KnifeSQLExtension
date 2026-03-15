using KnifeSQLExtension.Core.Models;
using SqlParser.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration
{
    public class Graph
    {
        public Dictionary<string, List<string>> Value { get; }

        public Graph(List<TableSchema> tableSchemas)
        {
            Value = new Dictionary<string, List<string>>();
            foreach(var table in tableSchemas) 
            {
                Value[table.Name] = GetReferencedTables(table);
            }
        }

        private List<string> GetReferencedTables(TableSchema table)
        {
            return table.Columns.Where(c => c.FkObject is not null).Select(c => c.FkObject.FkTableName).ToList();
        }
    }
}
