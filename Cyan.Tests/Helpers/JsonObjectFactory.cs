using System;
using UXRisk.Lib.Common.Models;

namespace Cyan.Tests.Helpers
{
    public static class JsonObjectFactory
    {
        public static JsonObject CreateJsonObjectForPostWithArray(string id = "someId", string parentId = "", string name = "someName")
        {
            var json = new JsonObject
            {
                {"id", id},
                {"name", name},
                {"parentId", parentId},
                {"PartitionKey", "PK"},
                {"RowKey", id},
                {"dragon_ids", new object[] {"1", "2", "3"}}
            };
            json.EnsureValidSystemProperties();
            return json;
        }

        public static JsonObject CreateJsonObjectForPost(string id = "someId", string parentId = "", string name = "someName")
        {
            var json = new JsonObject
            {
                {"id", id},
                {"name", name},
                {"parentId", parentId},
                {"PartitionKey", "PK"},
                {"dragon_ids", new object[] {"1", "2", "3"}},
                {"RowKey", id}
            };
            json.EnsureValidSystemProperties();
            return json;
        }

        public static JsonObject CreateJsonObject(DateTime aTimestamp, string id = "something")
        {
            const string valueString = "something";
            var json = new JsonObject { { "ETag", valueString }, { "Timestamp", aTimestamp }, { "id", id }, { "name", valueString } };
            json.EnsureValidSystemProperties();
            return json;
        }
    }
}