using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cyan
{
    internal static class CyanUtilities
    {
        public enum SupportedProtocols
        {
            Http,
            Https,
            NotSupported
        }

        private static readonly Regex ValidKeyFieldRegex = new Regex(@"^[^#/\\?]{0,1024}$", RegexOptions.Compiled);
        private static readonly Regex TableNameRegex = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}$", RegexOptions.Compiled);

        public static void ValidateKeyField(string keyField)
        {
            if (!ValidKeyFieldRegex.IsMatch(keyField))
                throw new ArgumentException();
        }

        public static void ValidateFieldType(Type type)
        {
            var typeName = type.Name;
            switch (typeName)
            {
                case "Byte[]":
                case "Boolean":
                case "DateTime":
                case "Double":
                case "Guid":
                case "Int32":
                case "Int64":
                case "String":
                    return;
                default:
                    throw new ArgumentException(string.Format("Type \"{0}\" is not supported.", type.FullName));
            }
        }

        private static bool IsValidTableName(string tableName)
        {
            return TableNameRegex.IsMatch(tableName);
        }

        public static void ValidateTableName(string tableName)
        {
            if (!IsValidTableName(tableName))
                throw new ArgumentException("Invalid table name.");
        }

        public static IDictionary<string, string> ParseConnectionStringKeyValues(string connectionString)
        {
            var elements = connectionString
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            var keyValues = elements
                .Select(SplitKeyValue)
                .Select(eTokens => new {Key = eTokens[0], Value = eTokens[1]});

            return keyValues.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.InvariantCultureIgnoreCase);
        }

        private static string[] SplitKeyValue(string element)
        {
            var indexOfSplitter = element.IndexOf('=');
            var elements = new string[2];
            elements[0] = element.Substring(0, indexOfSplitter);
            elements[1] = element.Substring(indexOfSplitter + 1);

            return elements;
        }

        public static SupportedProtocols ParseConnectionStringProtocol(
            IDictionary<string, string> connectionStringKeyValues)
        {
            string protocolValue;
            if (!connectionStringKeyValues.TryGetValue("DefaultEndpointsProtocol", out protocolValue))
                // when not specified, default to http
                return SupportedProtocols.Http;

            switch (protocolValue.ToLowerInvariant())
            {
                case "http":
                    return SupportedProtocols.Http;
                case "https":
                    return SupportedProtocols.Https;
                default:
                    return SupportedProtocols.NotSupported;
            }
        }

        internal static string FormatQuery(string partition, string row, string filter, int top, string[] fields,
            string nextPartition, string nextRow)
        {
            var hasPartition = !string.IsNullOrEmpty(partition);
            var hasRow = !string.IsNullOrEmpty(row);
            if (hasPartition ^ hasRow)
            {
                var indexer = hasPartition
                    ? string.Format("PartitionKey eq '{0}'", partition)
                    : string.Format("RowKey eq '{0}'", row);

                filter = string.IsNullOrEmpty(filter)
                    ? indexer
                    : string.Format("{0} and ({1})", indexer, filter);
            }

            if (string.IsNullOrEmpty(filter)
                && top <= 0
                && (fields == null || fields.Length == 0)
                && string.IsNullOrEmpty(nextPartition)
                && string.IsNullOrEmpty(nextRow))
                return null;

            return FormatQuery(!string.IsNullOrEmpty(filter) ? Tuple.Create("$filter", filter) : null,
                top > 0 ? Tuple.Create("$top", top.ToString(CultureInfo.InvariantCulture)) : null,
                (fields != null && fields.Length > 0) ? Tuple.Create("$select", string.Join(",", fields)) : null,
                !string.IsNullOrEmpty(nextPartition) ? Tuple.Create("NextPartitionKey", nextPartition) : null,
                !string.IsNullOrEmpty(nextRow) ? Tuple.Create("NextRowKey", nextRow) : null);
        }

        internal static string FormatQuery(params Tuple<string, string>[] queryParameters)
        {
            var ret = string.Join("&", queryParameters
                .Where(p => p != null)
                .Select(p => string.Format("{0}={1}", p.Item1, Uri.EscapeDataString(p.Item2)))
                .ToArray());

            return !string.IsNullOrEmpty(ret) ? ret : null;
        }

        internal static string FormatResource(string tableName, string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null)
                return tableName;

            return string.Format("{0}(PartitionKey='{1}',RowKey='{2}')",
                tableName,
                Uri.EscapeDataString(partitionKey),
                Uri.EscapeDataString(rowKey));
        }
    }
}