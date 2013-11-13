using System;
using UXRisk.Lib.Common.Models;

namespace Cyan.Tests.Helpers
{
    public static class JsonObjectFactory
    {
        public static JsonObject CreateJsonObject(DateTime aTimestamp, string id = "something")
        {
            const string valueString = "something";
            return new JsonObject {{"ETag", valueString}, {"Timestamp", aTimestamp}, {"id", id}, {"name", valueString}};
        }
    }
}