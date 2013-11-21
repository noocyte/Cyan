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
                {"RowKey", id}
            };

            return json;
        }

        public static JsonObject CreateJsonObject(DateTime aTimestamp, string id = "something")
        {
            const string valueString = "something";
            return new JsonObject { { "ETag", valueString }, { "Timestamp", aTimestamp }, { "id", id }, { "name", valueString } };
        }
    }
}