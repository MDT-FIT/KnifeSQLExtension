using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Core.Services.Database.Interfaces;
using Microsoft.Extensions.Logging;

namespace KnifeSQLExtension.Core.Services
{
    /// <summary>
    /// Manages DB snapshots and computes diffs between them.
    /// </summary>
    public class SnapshotService
    {
        private readonly ILogger<SnapshotService> _logger;

        // Key = "schema.tableName", Value = last saved snapshot
        private readonly Dictionary<string, DbSnapshot> _snapshots = new();

        public SnapshotService(ILogger<SnapshotService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Captures current data of a table and saves it as snapshot.
        /// </summary>
        public async Task CaptureSnapshotAsync(IDatabaseClient client, string tableName)
        {
            try
            {
                var rows = await client.GetDataAsync(tableName);
                var snapshot = new DbSnapshot
                {
                    TableName = tableName,
                    CapturedAt = DateTime.Now,
                    Rows = rows,
                    Columns = rows.Count > 0 ? rows[0].Keys.ToList() : new List<string>()
                };
                _snapshots[tableName] = snapshot;
                _logger.LogInformation("Snapshot captured for table {Table}", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture snapshot for {Table}", tableName);
            }
        }

        /// <summary>
        /// Captures snapshots for ALL tables in the database.
        /// </summary>
        public async Task CaptureAllSnapshotsAsync(IDatabaseClient client)
        {
            _snapshots.Clear();
            var tables = await client.GetTablesAsync();
            foreach (var table in tables)
            {
                await CaptureSnapshotAsync(client, table);
            }
        }

        /// <summary>
        /// Returns the saved snapshot for a table, or null if not captured yet.
        /// </summary>
        public DbSnapshot? GetSnapshot(string tableName)
        {
            return _snapshots.TryGetValue(tableName, out var snapshot) ? snapshot : null;
        }

        public IReadOnlyCollection<string> GetSnapshotTableNames()
        {
            return _snapshots.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Compares a snapshot with current live data.
        /// Returns list of changed cells: (rowIndex, columnName, oldValue, newValue)
        /// </summary>
        public List<DiffCell> ComputeDiff(
            DbSnapshot snapshot,
            List<Dictionary<string, object>> currentRows)
        {
            var diffs = new List<DiffCell>();

            int maxRows = Math.Max(snapshot.Rows.Count, currentRows.Count);

            for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
            {
                // Row exists in snapshot but not in current = deleted row
                if (rowIndex >= currentRows.Count)
                {
                    diffs.Add(new DiffCell
                    {
                        RowIndex = rowIndex,
                        ColumnName = "*",
                        OldValue = "(row existed)",
                        NewValue = "(row deleted)",
                        DiffType = DiffType.Deleted
                    });
                    continue;
                }

                // Row exists in current but not in snapshot = new row
                if (rowIndex >= snapshot.Rows.Count)
                {
                    diffs.Add(new DiffCell
                    {
                        RowIndex = rowIndex,
                        ColumnName = "*",
                        OldValue = "(row not existed)",
                        NewValue = "(row added)",
                        DiffType = DiffType.Added
                    });
                    continue;
                }

                var oldRow = snapshot.Rows[rowIndex];
                var newRow = currentRows[rowIndex];

                // Compare each column
                foreach (var column in oldRow.Keys.Union(newRow.Keys))
                {
                    var oldVal = oldRow.TryGetValue(column, out var ov) ? ov : null;
                    var newVal = newRow.TryGetValue(column, out var nv) ? nv : null;

                    string oldStr = oldVal == DBNull.Value || oldVal == null ? "NULL" : oldVal.ToString()!;
                    string newStr = newVal == DBNull.Value || newVal == null ? "NULL" : newVal.ToString()!;

                    if (oldStr != newStr)
                    {
                        diffs.Add(new DiffCell
                        {
                            RowIndex = rowIndex,
                            ColumnName = column,
                            OldValue = oldStr,
                            NewValue = newStr,
                            DiffType = DiffType.Modified
                        });
                    }
                }
            }

            return diffs;
        }
    }

    public class DiffCell
    {
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public DiffType DiffType { get; set; }
    }

    public enum DiffType
    {
        Modified,
        Added,
        Deleted
    }
}
