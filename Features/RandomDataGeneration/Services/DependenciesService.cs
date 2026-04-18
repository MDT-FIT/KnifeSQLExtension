using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static SqlParser.Ast.GrantObjects;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services
{
    public class DependenciesService
    {
        private readonly IDatabaseClient _client;
        private readonly TableService _tableService;
        private Graph _graph;

        public DependenciesService(IDatabaseClient client, TableService tableService)
        {
            _client = client;
            _tableService = tableService;
        }

        public async Task<List<string>> GetSortedTables()
        {
            Graph graph = _graph;
            var tables = await _tableService.GetTablesAsync();

            if(graph is null)
                graph = new(tables);

            return graph.SortedTables;
        }

        /// <summary>
        /// Finds additional tables that chosen ones depend on and that need to be populated 
        /// </summary>
        /// <param name="chosenTables">a list of tables to populate</param>
        /// <returns>a list of additional tables that need to be populated</returns>
        public async Task<List<string>> GetDependenciesDiffAsync(List<string> chosenTables)
        {
            var depTables = await GetDependenciesAsync(chosenTables);

            List<string> addTables = [];

            foreach(var table in depTables)
            {
                if(!chosenTables.Contains(table))
                {
                    bool empty = (await _tableService.GetTableDataAsync(table)).IsNullOrEmpty();

                    if(!empty)
                        continue;

                    addTables.Add(table);
                }
            }

            return addTables;
        }

        public async Task<List<string>> GetDependenciesAsync(List<string> chosenTables)
        {
            List<string> dependencies = [];
            var tables = await _tableService.GetTablesAsync();

            Graph graph = _graph;

            if(graph is null)
                graph = new(tables);

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
