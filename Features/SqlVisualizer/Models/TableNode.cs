using KnifeSQLExtension.Core.Models;

namespace KnifeSQLExtension.Features.SqlVisualizer.Models;

public class TableNode
{
    public TableSchema Metadata { get; set; }
    public int Row {  get; set; }
    public int Column {  get; set; }
}