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
        private readonly IDatabaseClient _client;

        public DependenciesService(IDatabaseClient client)
        {
            _client = client;
        }

        public async Task<List<string>> GetDependenciesAsync(List<TableSchema> tables, List<string> chosenTables)
        {
            List<string> dependencies = [];

            Graph graph = new(tables);

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
