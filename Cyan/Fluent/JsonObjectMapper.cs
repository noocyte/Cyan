using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UXRisk.Lib.Common.Models;

namespace Cyan.Fluent
{
    public static class JsonObjectMapper
    {
        public static JsonObject ToJsonObject(this CyanEntity ce)
        {
            var json = new JsonObject {{"ETag", ce.ETag}, {"Timestamp", ce.Timestamp}};
            foreach (var field in ce.Fields)
            {
                json[field.Key] =
                    IsArray(field)
                        ? JsonConvert.DeserializeObject<object[]>(field.Value.ToString())
                        : field.Value;
            }
            json.EnsureValidSystemProperties();
            return json;
        }

        private static bool IsArray(KeyValuePair<string, object> field)
        {
            return field.Key.Contains("_ids") &&
                   field.Value.ToString().StartsWith("[") &&
                   field.Value.ToString().EndsWith("]");
        }

        public static CyanEntity ToCyanEntity(this JsonObject oneObject)
        {
            if (oneObject == null)
                throw new ArgumentNullException("oneObject");

            return CyanEntity.FromEnumerable(oneObject);
        }
    }
}