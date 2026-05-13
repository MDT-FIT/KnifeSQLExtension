namespace KnifeSQLExtension.Core.Models.Constraints
{
    public sealed class ForeignConstraint : Constraint
    {
        public required string ReferencedTable { get; set; }

        public List<(string FromColumn, string ToColumn)> ColumnMappings { get; set; } = [];
    }
}