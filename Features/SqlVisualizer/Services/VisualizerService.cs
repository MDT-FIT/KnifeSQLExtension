using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using KnifeSQLExtension.Features.SqlVisualizer.Models;
using System.Text.Json;

namespace KnifeSQLExtension.Features.SqlVisualizer.Services;

public class VisualizerService
{
    private TableService _service;
    private int MAX_COLUMNS = 3;
    private int MAX_ROWS = 1;

    public VisualizerService(TableService service)
    {
        _service = service;
    }

    public async Task<Dictionary<string, TableNode>> CreateNodes()
    {
        List<TableSchema> tables = await _service.GetTablesAsync();

        MAX_ROWS = (int)Math.Ceiling(tables.Count / (double)MAX_COLUMNS);

        if (MAX_ROWS == 0) MAX_ROWS = 1;

        Dictionary<string, TableNode> nodes = new Dictionary<string, TableNode>();

        int currentRow = 1;
        int currentCol = 1;

        foreach (TableSchema table in tables)
        {
            nodes[table.TableName] = new TableNode { Metadata = table, Row = currentRow, Column = currentCol };

            currentCol++;

            if (currentCol > MAX_COLUMNS)
            {
                currentCol = 1;
                currentRow++;
            }
        }

        return nodes;
    }

    public string GetSerializedSchema(Dictionary<string, TableNode> nodes)
    {
        var schema = nodes.Select(kvp => new
        {
            Name = kvp.Key,
            Columns = kvp.Value.Metadata.Columns,
            Connections = kvp.Value.Metadata.ForeignConstraints.Select(fk => new
            {
                TargetTable = fk.ReferencedTable,
                Mappings = fk.ColumnMappings.Select(m => new
                {
                    Source = m.FromColumn,
                    Target = m.ToColumn
                })
            })
        });

        return JsonSerializer.Serialize(new { tables = schema });
    }
}