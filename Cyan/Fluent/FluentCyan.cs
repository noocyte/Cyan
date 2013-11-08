using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
            return resultEnumerable.Count() > 0
                    ? new Response<T>(HttpStatusCode.OK, resultEnumerable.First())
                    : new Response<T>(HttpStatusCode.NotFound, null as T);
        }

        public Response<dynamic[]> RetrieveAll()
        {
            var table = _tableClient[_tableName];

            var result = table.Query("PK");
            // ReSharper disable once CoVariantArrayConversion
            dynamic[] resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.0
            return resultEnumerable.Count() > 0
                    ? new Response<dynamic[]>(HttpStatusCode.OK, resultEnumerable)
                    : new Response<dynamic[]>(HttpStatusCode.NotFound, null as T[]);
        }

        public Response<dynamic[]> RetrieveBy(Func<dynamic, bool> predicate)
        {
            var table = _tableClient[_tableName];

            var result = table.Query("PK");
            // ReSharper disable once CoVariantArrayConversion
            dynamic[] resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.0
            if (resultEnumerable.Count(item => predicate(item)) > 0)
            {
                dynamic[] enumerable = resultEnumerable.Where(i=>predicate(i)).ToArray();
                return new Response<dynamic[]>(HttpStatusCode.OK, enumerable);
            }

            else return new Response<dynamic[]>(HttpStatusCode.NotFound, null as T[]);
        }

    }
}
