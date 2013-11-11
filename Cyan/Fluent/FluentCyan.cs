using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cyan.Interfaces;

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

        public Response<CyanEntity> Retrieve(string id)
        {
            if (string.IsNullOrEmpty(id)) 
                throw new ArgumentNullException("id");

            var table = _tableClient[_tableName];
            var result = table.Query("PK", id).ToList();

            // ReSharper disable once UseMethodAny.0
            return result.Count() > 0
                ? new Response<CyanEntity>(HttpStatusCode.OK, result.First())
                : new Response<CyanEntity>(HttpStatusCode.NotFound, null);
        }

        public Response<IEnumerable<CyanEntity>> RetrieveAll()
        {
            var table = _tableClient[_tableName];
            var result = table.Query("PK").ToList();

            // ReSharper disable once UseMethodAny.0
            return result.Count() > 0
                ? new Response<IEnumerable<CyanEntity>>(HttpStatusCode.OK, result)
                : new Response<IEnumerable<CyanEntity>>(HttpStatusCode.NotFound, null);
        }
    }
}