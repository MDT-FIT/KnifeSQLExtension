namespace KnifeSQLExtension.Core;

// Class deisgned to inspect SQL queries before they are executed
public static class Parser
{
    // Method scans if SQL query contains dangerous operations and user warning
    public static string CheckForWarnings(string sqlQuery)
    {
        // if query is empty return it
        if (string.IsNullOrWhiteSpace(sqlQuery)) 
            return string.Empty;

        // convert to uppercase for easier keyword mathcing
        string upperQuery = sqlQuery.ToUpperInvariant();

        // make a warning for user if query 
        // has DROP TABLE/DATABASE (destroying whole table/DB)
        // that can lead to data destruction
        if (upperQuery.Contains("DROP TABLE") || upperQuery.Contains("DROP DATABASE"))
            return "⚠️ Attention: Цей запит знищить таблицю або базу даних!";

        // warning for missing WHERE clauses in DELETE/UPDATE statements
        //  DELETE FROM combination without WHERE keyword (deleting all records from some table)
        if (upperQuery.Contains("DELETE FROM") && !upperQuery.Contains("WHERE"))
            return "⚠️ Attention: Ви намагаєтеся видалити ВСІ записи з таблиці (немає умови WHERE)!";

        // contains UPDATE without specifing WHERE keyword (updating all records in table)
        if (upperQuery.Contains("UPDATE") && !upperQuery.Contains("WHERE"))
            return "⚠️ Attention: Ви оновите ВСІ записи в таблиці (немає умови WHERE)!";

        return string.Empty; // if all is OK - return empty string
    }
}