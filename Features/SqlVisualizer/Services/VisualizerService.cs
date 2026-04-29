using System.Text;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Features.RandomDataGeneration.Services;
using KnifeSQLExtension.Features.SqlVisualizer.Constants;
using KnifeSQLExtension.Features.SqlVisualizer.Models;

namespace KnifeSQLExtension.Features.SqlVisualizer.Services;

public class VisualizerService
{
    private TableService _service;
    private readonly SvgStyleConfig _config;
    private int MAX_COLUMNS = 3;
    private int MAX_ROWS = 1;

    public VisualizerService(TableService service, SvgStyleConfig? config = null)
    {
        _service = service;
        _config = config ?? SvgStyleConfig.Default;
    }

    public async Task<Dictionary<string, TableNode>> CreateNodes()
    {
        var tables = await _service.GetTablesAsync();
        
        MAX_ROWS = (int)Math.Ceiling(tables.Count / (double)MAX_COLUMNS);
        
        if (MAX_ROWS == 0) MAX_ROWS = 1;

        var nodes = new Dictionary<string, TableNode>();
        int currentRow = 1;
        int currentCol = 1;
        
        foreach (var table in tables)
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

    public string GenerateSvgNodes(Dictionary<string, TableNode> nodes)
    {
        if (nodes.Count == 0) return "<p>No tables to display.</p>";
        
        var html = new StringBuilder();
        
        string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dist", "index.html");

        return htmlPath;
        
        // html.AppendLine(@"
        //     <style>
        //         .diagram-container {
        //             position: relative;
        //             width: 100%;
        //             background: #1e1e1e; /* Dark theme */
        //             color: #d4d4d4;
        //             font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        //             padding: 40px;
        //             box-sizing: border-box;
        //         }
        //         .grid {
        //             display: grid;
        //             /* Dynamically set based on our max columns */
        //             gap: 60px;
        //             position: relative;
        //             z-index: 2; /* Keep tables above the lines */
        //             width: fit-content;
        //         }
        //         .table-card {
        //             background: #252526;
        //             border: 1px solid #454545;
        //             border-radius: 6px;
        //             box-shadow: 0 4px 6px rgba(0,0,0,0.3);
        //             padding: 0;
        //             overflow: hidden;
        //             width: 200px;
        //         }
        //         .table-header {
        //             background: #007acc; /* VS Code Blue */
        //             color: white;
        //             padding: 8px 12px;
        //             font-weight: bold;
        //             text-align: center;
        //         }
        //         .table-col {
        //             padding: 6px 12px;
        //             border-bottom: 1px solid #333;
        //             display: flex;
        //             justify-content: space-between;
        //             font-size: 13px;
        //         }
        //         .table-col:last-child { border-bottom: none; }
        //         .col-type { color: #569cd6; font-size: 11px; } /* Blueish type */
        //         
        //         /* The SVG Canvas covers the grid area */
        //         .relations-canvas {
        //             pointer-events: none; /* Let clicks pass through to tables */
        //             z-index: 1; /* Keep lines behind the tables */
        //         }
        //         .relation-line {
        //             stroke: #808080;
        //             stroke-width: 2;
        //             stroke-dasharray: 4; /* Makes the line dotted */
        //         }
        //     </style>");
        //
        // html.AppendLine("<div class='diagram-container'>");
        // html.AppendLine($"<div class='grid' style='grid-template-columns: repeat({MAX_COLUMNS}, 1fr);'>");
        //
        // foreach (var node in nodes.Values)
        // {
        //     html.Append(DrawTable(node));
        // }
        //
        // DrawRelations(html, nodes);
        //
        // html.AppendLine("</div>");
        // html.AppendLine("</div>");
        //
        // Console.WriteLine(html.ToString());
        //
        // return html.ToString();
    }

    private string DrawTable(TableNode node)
    {
        return $@"
    <div class='table-card' style='grid-row: {node.Row}; grid-column: {node.Column};'>
        <div class='table-header'>{node.Metadata.TableName}</div>
        {string.Join("", node.Metadata.Columns.Select(c => $"<div class='table-col'>{c.Name} <span class='col-type'>{c.SqlType}</span></div>"))}
    </div>";
    }

    private void DrawRelations(StringBuilder html, Dictionary<string, TableNode> nodes)
    {
        // Calculate grid dimensions
        const int tableWidth = 200;
        const int tableHeight = 150; // Approximate
        const int gapSize = 60;
        const int leftPadding = 40;
        const int topPadding = 40;
        
        double totalWidth = (MAX_COLUMNS * (tableWidth + gapSize)) + (2 * leftPadding);
        double totalHeight = (MAX_ROWS * (tableHeight + gapSize)) + (2 * topPadding);
        
        html.AppendLine($"<svg class='relations-canvas' width='{totalWidth}' height='{totalHeight}' style='position: absolute; top: 0; left: 0;'>");
            
        foreach (var node in nodes.Values)
        {
            foreach (var fk in node.Metadata.ForeignConstraints)
            {
                // Extract table name from ReferencedTable (format: "schema.table" or "table")
                string refTableName = fk.ReferencedTable.Contains(".") 
                    ? fk.ReferencedTable.Split(".").Last() 
                    : fk.ReferencedTable;
                
                // Try to find the target table, trying both the qualified name and just the table name
                TableNode? targetNode = null;
                if (nodes.TryGetValue(refTableName, out targetNode) || 
                    nodes.TryGetValue(fk.ReferencedTable, out targetNode))
                {
                    // Calculate pixel positions (center of each table card)
                    double startX = leftPadding + (node.Column - 1) * (tableWidth + gapSize) + (tableWidth / 2);
                    double startY = topPadding + (node.Row - 1) * (tableHeight + gapSize) + (tableHeight / 2);
                    double endX = leftPadding + (targetNode.Column - 1) * (tableWidth + gapSize) + (tableWidth / 2);
                    double endY = topPadding + (targetNode.Row - 1) * (tableHeight + gapSize) + (tableHeight / 2);

                    html.AppendLine($"<line class='relation-line' x1='{startX}' y1='{startY}' x2='{endX}' y2='{endY}' />");
                }
            }
        }
        
        html.AppendLine("</svg>");
    }
}