using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cyan.Interfaces;

namespace Cyan.Fluent
{
    public class FluentCyan<T> where T : class
    {
        private readonly ICyanClient _tableClient;
        private string _tableName;

        public FluentCyan(ICyanClient tableClient)
        {
            _tableClient = tableClient;
        }

        public FluentCyan<T> FromTable(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            _tableName = tableName;
            _tableClient.TryCreateTable(_tableName);
            return this;
        }

        public Response<T> Retrieve(string id)
        {
            var table = _tableClient[_tableName];

            var result = table.Query("PK", id);
            // ReSharper disable once CoVariantArrayConversion
            dynamic[] resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.0
            if (resultEnumerable.Count() > 0)
                return new Response<T>(HttpStatusCode.OK, resultEnumerable.First());
            return new Response<T>(HttpStatusCode.NotFound, null);
        }

        public Response<IEnumerable<T>> RetrieveAll()
        {
            var table = _tableClient[_tableName];

            var result = table.Query("PK");
            // ReSharper disable once CoVariantArrayConversion
            dynamic[] resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.0
            if (resultEnumerable.Count() > 0)
                return new Response<IEnumerable<T>>(HttpStatusCode.OK, resultEnumerable as IEnumerable<T>);
            return new Response<IEnumerable<T>>(HttpStatusCode.NotFound, null);
        }
    }
}
