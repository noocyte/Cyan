using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cyan.Interfaces;
using UXRisk.Lib.Common.Models;

namespace Cyan.Fluent
{
    public class FluentCyan
    {
        private readonly ICyanClient _tableClient;
        private string _tableName;

        public FluentCyan(ICyanClient tableClient)
        {
            _tableClient = tableClient;
        }

        public FluentCyan FromTable(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            _tableName = tableName;
            _tableClient.TryCreateTable(_tableName);
            return this;
        }

        public Response<JsonObject> Retrieve(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            var table = _tableClient[_tableName];
            var result = table.Query("PK", id).ToList();
            var json = new JsonObject();
            var status = HttpStatusCode.NotFound;

            // ReSharper disable once UseMethodAny.0
            if (result.Count() > 0)
            {
                var fields = result.First().Fields;
                json.AddRange(fields);
                status = HttpStatusCode.OK;
            }

            return new Response<JsonObject>(status, json);
        }

        public Response<IEnumerable<JsonObject>> RetrieveAll()
        {
            var table = _tableClient[_tableName];
            var result = table.Query("PK").ToList();

            var status = HttpStatusCode.NotFound;
            var listOfJson = new List<JsonObject>();

            if (result.Count() > 0)
            {
                foreach (var ce in result)
                {
                    var fields = ce.Fields;
                    var json = new JsonObject();
                    json.AddRange(fields);    
                    
                    listOfJson.Add(json);
                }
                
                status = HttpStatusCode.OK;
            }

            return new Response<IEnumerable<JsonObject>>(status, listOfJson);
        }
    }
}