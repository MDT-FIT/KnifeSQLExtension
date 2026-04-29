namespace KnifeSQLExtension.Core.Models.Constraints
{
    public class UniqueConstraint : Constraint
    {
        public List<string> Columns { get; set; } = [];
    }
}