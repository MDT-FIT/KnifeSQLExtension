using KnifeSQLExtension.Core.Models;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text.Json;

namespace KnifeSQLExtension.Core.Services.Database.Implementations
{
    // Implementation of IDatabaseClient for Microsoft SQL Server
    public class MsSqlDatabaseService : AbstractDatabaseService
    {
        // Gets the provider type as MS SQL.
        public override Type Type { get; } = Type.MsSql;

        // Creates a new instance of connection for SQL Server interaction
        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        // Retrieves a list of all user-defined tables from the database using INFORMATION_SCHEMA
        public override async Task<List<string>> GetTablesAsync()
        {
            string query = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var result = await ExecuteQueryAsync(query);

            var tables = new List<string>();
            foreach (var row in result)
            {
                if (row.ContainsKey("TABLE_SCHEMA") && row.ContainsKey("TABLE_NAME"))
                {
                    tables.Add($"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}");
                }
            }
            return tables;
        }

        // Retrieves comprehensive schema information for a specific table, including columns, 
        // primary keys, identities, and foreign key relationships.
        public override async Task<TableSchema> GetTableSchemaAsync(string tableName, string schema = "dbo")
        {
            string query = MSSqlServerTableSchemaQuery.Query(tableName, schema);
            var data = await ExecuteQueryAsync(query);
            var tableSchema = new TableSchema(tableName);

            foreach (var row in data)
            {
                var colSchema = new ColumnSchema();

                string? name = row[nameof(colSchema.Name)].ToString();
                string? type = row[nameof(colSchema.SqlType)].ToString();
                int maxLength = Convert.ToInt32(row[nameof(colSchema.MaxLength)].ToString());
                int isNullable = Convert.ToInt32(row[nameof(colSchema.IsNullable)].ToString());
                int isPrimaryKey = Convert.ToInt32(row[nameof(colSchema.IsPrimaryKey)].ToString());
                int isIdentity = Convert.ToInt32(row[nameof(colSchema.IsIdentity)].ToString());
                int isComputed = Convert.ToInt32(row[nameof(colSchema.IsComputed)].ToString());
                int hasDefault = Convert.ToInt32(row[nameof(colSchema.HasDefault)].ToString());
                var fkRaw = row["ForeignKeysJson"];
                string? jsonString = (fkRaw == DBNull.Value || fkRaw == null) ? null : fkRaw.ToString();

                ArgumentNullException.ThrowIfNull(name);
                ArgumentNullException.ThrowIfNull(type);

                colSchema.Name = name;
                colSchema.SqlType = type;
                colSchema.MaxLength = maxLength == 0 ? null : maxLength;
                colSchema.IsNullable = isNullable == 1;
                colSchema.IsPrimaryKey = isPrimaryKey == 1;
                colSchema.IsIdentity = isIdentity == 1;
                colSchema.IsComputed = isComputed == 1;
                colSchema.HasDefault = hasDefault == 1;
                colSchema.FkObject = string.IsNullOrWhiteSpace(jsonString) ? null : JsonSerializer.Deserialize<FkObject>(jsonString);

                tableSchema.Columns.Add(colSchema);
            }

            return tableSchema;
        }

        // Fetches all available schema names within the current MS SQL database
        // Return a list of schema names from sys.schemas
        public override async Task<List<string>> GetDatabaseSchemasAsync()
        {
            string query = "SELECT name, schema_id FROM sys.schemas;";
            var result = await ExecuteQueryAsync(query);

            List<string> schemas = [];
            foreach (var row in result)
            {
                if (row.ContainsKey("name") && row["name"] != null)
                {
                    schemas.Add(row["name"].ToString());
                }
            }
            return schemas;
        }
    }
}