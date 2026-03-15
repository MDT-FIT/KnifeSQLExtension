using KnifeSQLExtension.Core.Models;


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
            return table.Columns
                .Where(c => c.FkObject is not null && !c.IsNullable) // only hard dependencies
                .Select(c => c.FkObject.FkTableName)
                .ToList();
        }

        public List<string> GetSortedTables()
        {
            Dictionary<string, int> indegree = [];
            List<string> sortedTables = [];
            Queue<string> queue = [];

            // Initialize dependencies count for each table
            foreach(var item in Value)
                indegree[item.Key] = item.Value.Count;

            // Enque tables without dependencies first
            foreach(var item in indegree)
                if(indegree[item.Key] == 0)
                    queue.Enqueue(item.Key);

            // Resolve dependencies in order from least to most dependent
            while(queue.Count > 0)
            {
                string top = queue.Dequeue();
                sortedTables.Add(top);
                foreach(string next in Value[top])
                {
                    indegree[next]--;
                    if(indegree[next] == 0)
                        queue.Enqueue(next);
                }
            }

            return sortedTables;
        }
    }
}
