using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SqlParser.Ast.GrantObjects;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class DependenciesService
    {
        private IDatabaseClient _client;

        public DependenciesService(IDatabaseClient client)
        {
            _client = client;
        }

        public async Task<List<string>> GetDependenciesAsync(List<string> chosenTables)
        {
            List<string> dependencies = [];

            var tables = (await _client.GetTablesAsync()).Select(t => t.Split('.')[1]).ToList();

            List<TableSchema> schemas = [];

            foreach(var table in tables)
            {
                schemas.Add(await _client.GetTableSchemaAsync(table));
            }

            Graph graph = new(schemas);

            foreach(var table in chosenTables)
            {
                await GetDependenciesAsync(table, graph, dependencies);
            }

            return dependencies;
        }

        private async Task GetDependenciesAsync(string targetedTable, Graph graph, List<string> currentDependencies)
        {
            ArgumentNullException.ThrowIfNull(_client);

            if(currentDependencies.IndexOf(targetedTable) != -1)
                return;

            var depTables = graph.Body[targetedTable];

            currentDependencies.Add(targetedTable);

            foreach(string dep in depTables)
            {
                await GetDependenciesAsync(dep, graph, currentDependencies);
            }
        }
    }
}
