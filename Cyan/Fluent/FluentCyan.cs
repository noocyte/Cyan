using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cyan.Interfaces;
using UXRisk.Lib.Common.Models;

namespace Cyan.Fluent
{
    public class FluentCyan : IFluentCyan
    {
        private readonly ICyanClient _tableClient;
        private string _tableName;

        public FluentCyan(ICyanClient tableClient)
        {
            _tableClient = tableClient;
        }

        public IFluentCyan IntoTable(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            _tableName = tableName;
            return this;
        }

        public IFluentCyan FromTable(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            _tableName = tableName;
            return this;
        }

        public async Task<Response<JsonObject>> PostAsync(JsonObject json)
        {
            if (json == null)
                throw new ArgumentNullException("json");

            var table = await DefineTable().ConfigureAwait(false);
            var entity = json.ToCyanEntity();
            var result = await table.Insert(entity).ConfigureAwait(false);

            return new Response<JsonObject>(HttpStatusCode.Created, result.ToJsonObject());
        }

        public async Task<Response<JsonObject>> GetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            var table = await DefineTable().ConfigureAwait(false);
            var items = await table.Query("PK", id, filter: "sys_deleted ne true").ConfigureAwait(false);
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

        public async Task<Response<IEnumerable<JsonObject>>> GetAllAsync()
        {
            var table = await DefineTable().ConfigureAwait(false);
            var items = await table.Query("PK").ConfigureAwait(false);
            var result = items.ToList();

            var status = HttpStatusCode.NotFound;
            var listOfJson = new List<JsonObject>();

            // ReSharper disable once UseMethodAny.0
            if (result.Count() > 0)
            {
                listOfJson.AddRange(result.Select(ce => ce.ToJsonObject()));
                status = HttpStatusCode.OK;
            }

            return new Response<IEnumerable<JsonObject>>(status, listOfJson);
        }

        public async Task<Response<JsonObject>> MergeAsync(JsonObject json)
        {
            if (json == null)
                throw new ArgumentNullException("json");

            var table = await DefineTable().ConfigureAwait(false);
            var entity = json.ToCyanEntity();
            var result = await table.Merge(entity).ConfigureAwait(false);

            return new Response<JsonObject>(HttpStatusCode.OK, result.ToJsonObject());
        }

        public async Task<Response<JsonObject>> DeleteAsync(JsonObject json)
        {
            if (json == null)
                throw new ArgumentNullException("json");

            var table = await DefineTable().ConfigureAwait(false);
            var entity = json.ToCyanEntity();
            var result = new JsonObject();

            await table.Delete(entity).ConfigureAwait(false);

            return new Response<JsonObject>(HttpStatusCode.OK, result);
        }

        internal async Task<ICyanTable> DefineTable()
        {
            _tableName = _tableName.ToLowerInvariant();
            await _tableClient.TryCreateTable(_tableName).ConfigureAwait(false);
            var table = _tableClient[_tableName];
            return table;
        }
    }
}