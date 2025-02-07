using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Extensions;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Pomelo.EntityFrameworkCore.MySql.Metadata.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace GZY.EFCoreCompare.MySql
{
    /// <summary>
    /// 重写MySql获取索引的方法
    /// </summary>
    public class GzyMySqlDatabaseModelFactory : MySqlDatabaseModelFactory
    {
        private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly IMySqlOptions _options;


        public GzyMySqlDatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger, IRelationalTypeMappingSource typeMappingSource, IMySqlOptions options) : base(logger, typeMappingSource, options)
        {
            _logger = logger;
            _typeMappingSource = typeMappingSource;
            _options = options;
        }
        private const string GetCreateTableStatementQuery = @"SHOW CREATE TABLE `{0}`.`{1}`;";
        private static string GetCreateTableQuery(DbConnection connection, DatabaseTable table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = string.Format(GetCreateTableStatementQuery, connection.Database, table.Name);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return reader.GetValueOrDefault<string>("Create Table");
            }

            throw new InvalidOperationException("The statement 'SHOW CREATE TABLE' did not return any results.");
        }
        private const string GetIndexesQuery = @"SELECT `INDEX_NAME`,
     `NON_UNIQUE`,
     GROUP_CONCAT(`COLUMN_NAME` ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS `COLUMNS`,
     GROUP_CONCAT(IFNULL(`SUB_PART`, 0) ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS `SUB_PARTS`,
     `INDEX_TYPE`
     FROM `INFORMATION_SCHEMA`.`STATISTICS`
     WHERE `TABLE_SCHEMA` = '{0}'
     AND `TABLE_NAME` = '{1}'
     AND `INDEX_NAME` <> 'PRIMARY'
     GROUP BY `INDEX_NAME`, `NON_UNIQUE`, `INDEX_TYPE`;";
        protected virtual void GetIndexes(
                   DbConnection connection,
                   IReadOnlyList<DatabaseTable> tables,
                   Func<string, string, bool> tableFilter)
        {
            foreach (var table in tables)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(GetIndexesQuery, connection.Database, table.Name);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var columns = reader.GetValueOrDefault<string>("COLUMNS").Split(',').Select(s => GetColumn(table, s)).ToList();

                                // Reuse an existing index over the same columns, to workaround an EF Core
                                // bug (EF#11846 and #1189).
                                // The columns could be in a different order.
                                var index = new DatabaseIndex
                                {
                                    Table = table,
                                    Name = reader.GetValueOrDefault<string>("INDEX_NAME"),
                                };

                                index.IsUnique |= !reader.GetValueOrDefault<bool>("NON_UNIQUE");

                                var prefixLengths = reader.GetValueOrDefault<string>("SUB_PARTS")
                                    .Split(',')
                                    .Select(int.Parse)
                                    .ToArray();

                                var hasPrefixLengths = prefixLengths.Any(n => n > 0);
                                if (hasPrefixLengths)
                                {
                                    if (index.Columns.Count <= 0)
                                    {
                                        // If this is the first time an index with this set of columns is being defined,
                                        // then use whatever prefices have been declared.
                                        index[MySqlAnnotationNames.IndexPrefixLength] = prefixLengths;
                                    }
                                    else
                                    {
                                        // Use no prefix length at all or the highest prefix length for a given column
                                        // from all indexes with the same set of columns.
                                        var existingPrefixLengths = (int[])index[MySqlAnnotationNames.IndexPrefixLength];

                                        // Bring the prefix length in the same column order used for the already
                                        // existing prefix lengths from a previous index with the same set of columns.
                                        var newPrefixLengths = index.Columns
                                            .Select(indexColumn => columns.IndexOf(indexColumn))
                                            .Select(
                                                i => i < prefixLengths.Length
                                                    ? prefixLengths[i]
                                                    : 0)
                                            .Zip(
                                                existingPrefixLengths, (l, r) => l == 0 || r == 0
                                                    ? 0
                                                    : Math.Max(l, r))
                                            .ToArray();

                                        index[MySqlAnnotationNames.IndexPrefixLength] = newPrefixLengths.Any(p => p > 0)
                                            ? newPrefixLengths
                                            : null;
                                    }
                                }
                                else
                                {
                                    // If any index (with the same columns) is defined without index prefices at all,
                                    // then don't use any prefices.
                                    index[MySqlAnnotationNames.IndexPrefixLength] = null;
                                }

                                var indexType = reader.GetValueOrDefault<string>("INDEX_TYPE");

                                if (string.Equals(indexType, "spatial", StringComparison.OrdinalIgnoreCase))
                                {
                                    index[MySqlAnnotationNames.SpatialIndex] = true;
                                }

                                if (string.Equals(indexType, "fulltext", StringComparison.OrdinalIgnoreCase))
                                {
                                    index[MySqlAnnotationNames.FullTextIndex] = true;
                                }

                                if (index.Columns.Count <= 0)
                                {
                                    foreach (var column in columns)
                                    {
                                        index.Columns.Add(column);
                                    }

                                    table.Indexes.Add(index);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Logger.LogError(ex, "Error assigning index for {table}.", table.Name);
                            }
                        }
                    }
                }

                //
                // Post-process the full-text indices, because we cannot open to data readers over the same connection at the same time.
                //

                var fullTextIndexes = table.Indexes
                    .Where(i => ((bool?)i[MySqlAnnotationNames.FullTextIndex]).GetValueOrDefault())
                    .ToList();

                if (fullTextIndexes.Any())
                {
                    var createTableQuery = GetCreateTableQuery(connection, table);
                    var fullTextParsers = GetFullTextParsers(createTableQuery);

                    foreach (var fullTextIndex in fullTextIndexes)
                    {
                        if (fullTextParsers.TryGetValue(fullTextIndex.Name, out var fullTextParser))
                        {
                            fullTextIndex[MySqlAnnotationNames.FullTextParser] = fullTextParser;
                        }
                    }
                }
            }
        }
        private static Dictionary<string, string> GetFullTextParsers(string createTableQuery)
           => Regex.Matches(
                   createTableQuery,
                   @"\s*FULLTEXT\s+(?:INDEX|KEY)\s+(?:`(?<IndexName>(?:[^`]|``)+)`|(?<IndexName>\S+)).*WITH\s+PARSER\s+(?:`(?<FullTextParser>(?:[^`]|``)+)`|(?<FullTextParser>\S+))",
                   RegexOptions.IgnoreCase)
               .Where(m => m.Success)
               .ToDictionary(
                   m => m.Groups["IndexName"].Value.Replace("``", "`"),
                   m => m.Groups["FullTextParser"].Value.Replace("``", "`"));
        protected virtual ReferentialAction? ConvertToReferentialAction(string onDeleteAction)
         => onDeleteAction.ToUpperInvariant() switch
         {
             "RESTRICT" => ReferentialAction.Restrict,
             "CASCADE" => ReferentialAction.Cascade,
             "SET NULL" => ReferentialAction.SetNull,
             "NO ACTION" => ReferentialAction.NoAction,
             _ => null
         };

        private DatabaseColumn GetColumn(DatabaseTable table, string columnName)
            => FindColumn(table, columnName) ??
               throw new InvalidOperationException($"Could not find column '{columnName}' in table '{table.Name}'.");

        private DatabaseColumn FindColumn(DatabaseTable table, string columnName)
            => table.Columns.SingleOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal)) ??
               table.Columns.SingleOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
    }


    internal static class DbDataReaderExtension
    {
        public static T GetValueOrDefault<T>([NotNull] this DbDataReader reader, [NotNull] string name)
        {
            var idx = reader.GetOrdinal(name);
            return reader.IsDBNull(idx)
                ? default
                : reader.GetFieldValue<T>(idx);
        }

        public static bool HasName(this DbDataReader reader, string columnName)
            => Enumerable.Range(0, reader.FieldCount)
                .Any(i => string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase));
    }
}
