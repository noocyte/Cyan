using UXRisk.Lib.Common.Models;

namespace Cyan.Fluent
{
    public static class JsonObjectMapper
    {
        public static JsonObject ToJsonObject(this CyanEntity ce)
        {
            var json = new JsonObject() {{"ETag", ce.ETag}, {"Timestamp", ce.Timestamp}};
            foreach (var field in ce.Fields)
            {
                json.Add(field.Key, field.Value);
            }

            return json;
        }
    }
}