using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        private void DefineTable(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            _tableName = tableName;
            _tableClient.TryCreateTable(_tableName).ConfigureAwait(false);
        }

        public FluentCyan IntoTable(string tableName)
        {
            DefineTable(tableName);
            return this;
        }

        public FluentCyan FromTable(string tableName)
        {
            DefineTable(tableName);
            return this;
        }

        public async Task<Response<JsonObject>> PostAsync(JsonObject json)
        {
            if (json == null)
                throw new ArgumentNullException("json");

            var table = _tableClient[_tableName];
            var entity = json.ToCyanEntity();
            var result = await table.Insert(entity).ConfigureAwait(false);

            return new Response<JsonObject>(HttpStatusCode.Created, result.ToJsonObject());
        }

        public async Task<Response<JsonObject>> RetrieveAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            var table = _tableClient[_tableName];
            var items = await table.Query("PK", id).ConfigureAwait(false);
            var result = items.ToList();
            var json = new JsonObject();
            var status = HttpStatusCode.NotFound;

            // ReSharper disable once UseMethodAny.0
            if (result.Count() > 0)
            {
                json = result.First().ToJsonObject();
                status = HttpStatusCode.OK;
            }

            return new Response<JsonObject>(status, json);
        }

        public async Task<Response<IEnumerable<JsonObject>>> RetrieveAllAsync()
        {
            var table = _tableClient[_tableName];
            var items = await table.Query("PK").ConfigureAwait(false);
            var result = items.ToList();

            var status = HttpStatusCode.NotFound;
            var listOfJson = new List<JsonObject>();

            // ReSharper disable once UseMethodAny.0
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