using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
            var table = _tableClient[_tableName];

            var result = table.Query("PK", id);
            // ReSharper disable once CoVariantArrayConversion
            dynamic[] resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.0
            return resultEnumerable.Count() > 0
                    ? new Response<CyanEntity>(HttpStatusCode.OK, resultEnumerable.First())
                    : new Response<CyanEntity>(HttpStatusCode.NotFound, null);
        }

        public Response<IEnumerable<CyanEntity>> RetrieveAll()
        {
            var table = _tableClient[_tableName];

            var result = table.Query("PK");
            // ReSharper disable once CoVariantArrayConversion
            var resultEnumerable = result as IEnumerable<CyanEntity>;
            // ReSharper disable once UseMethodAny.0
            var cyanEntities = resultEnumerable as CyanEntity[] ?? resultEnumerable.ToArray();
            return cyanEntities.Count() > 0
                    ? new Response<IEnumerable<CyanEntity>>(HttpStatusCode.OK, cyanEntities)
                    : new Response<IEnumerable<CyanEntity>>(HttpStatusCode.NotFound, null);
        }
    }
}
