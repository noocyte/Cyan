using System;
using System.Collections.Generic;
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
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var keyValues = elements
                .Select(SplitKeyValue)
                .Select(eTokens => new { Key = eTokens[0], Value = eTokens[1] });

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
    }
}