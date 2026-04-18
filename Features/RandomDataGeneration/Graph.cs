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

        /// <summary>
        /// Populates the Body dictionary with referenced tables for each provided table schema.
        /// </summary>
        /// <param name="tableSchemas">A list of table schemas to process. Cannot be null.</param>
        private void CreateBody(List<TableSchema> tableSchemas)
        {
            foreach(var table in tableSchemas)
            {
                Body[table.FullName] = GetReferencedTables(table);
            }
        }

        /// <summary>
        /// Retrieves the names of tables that are referenced by non-nullable foreign key columns in the specified table
        /// schema.
        /// </summary>
        /// <remarks>Only direct, non-nullable foreign key dependencies are included. Nullable foreign key
        /// columns are not considered hard dependencies and are excluded from the result.</remarks>
        /// <param name="table">The table schema to analyze for referenced tables. Cannot be null.</param>
        /// <returns>A list of table names that are referenced by non-nullable foreign key columns. The list is empty if there
        /// are no such references.</returns>
        private List<string> GetReferencedTables(TableSchema table)
        {
            return table.Columns
                .Where(c => c.FkObject is not null && !c.IsNullable) // only hard dependencies
                .Select(c => c.FkObject.FkFullTableName)
                .ToList();
        }

        /// <summary>
        /// Sorts the tables in dependency order so that each table appears after all tables it depends on.
        /// </summary>
        /// <remarks>This method performs a topological sort based on the dependencies defined in the Body
        /// collection. Tables with no dependencies are placed first in the sorted list. This is typically used to
        /// ensure that tables are processed in an order that respects their dependency relationships. If the dependency
        /// graph contains cycles, not all tables may be included in the sorted result.</remarks>
        private void SortTables()
        {
            Dictionary<string, int> indegree = [];
            Queue<string> queue = [];

            // Initialize dependencies count for each table
            foreach(var item in Body)
                indegree[item.Key] = 0;

            // Count how many tables point to each table
            foreach(var item in Body)
                foreach(var dep in item.Value)
                    indegree[dep]++;

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
