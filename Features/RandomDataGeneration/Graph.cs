using KnifeSQLExtension.Core.Models;


namespace KnifeSQLExtension.Features.RandomDataGeneration
{
    public class Graph
    {
        public Dictionary<string, List<string>> Body { get; }
        public List<string> SortedTables { get; }

        public Graph(List<TableSchema> tableSchemas)
        {
            Body = [];
            SortedTables = [];

            CreateBody(tableSchemas);
            SortTables();
        }

        private void CreateBody(List<TableSchema> tableSchemas)
        {
            foreach(var table in tableSchemas)
            {
                Body[table.Name] = GetReferencedTables(table);
            }
        }

        private List<string> GetReferencedTables(TableSchema table)
        {
            return table.Columns
                .Where(c => c.FkObject is not null && !c.IsNullable) // only hard dependencies
                .Select(c => c.FkObject.FkTableName)
                .ToList();
        }

        private void SortTables()
        {
            Dictionary<string, int> indegree = [];
            Queue<string> queue = [];

            // Initialize dependencies count for each table
            foreach(var item in Body)
                indegree[item.Key] = item.Value.Count;

            // Enque tables without dependencies first
            foreach(var item in indegree)
                if(indegree[item.Key] == 0)
                    queue.Enqueue(item.Key);

            // Resolve dependencies in order from least to most dependent
            while(queue.Count > 0)
            {
                string top = queue.Dequeue();
                SortedTables.Add(top);
                foreach(string next in Body[top])
                {
                    indegree[next]--;
                    if(indegree[next] == 0)
                        queue.Enqueue(next);
                }
            }
        }
    }
}
