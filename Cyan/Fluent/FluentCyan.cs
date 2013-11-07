using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cyan.Interfaces;

namespace Cyan.Fluent
{
    public class FluentCyan<T> where T : class 
    {
        public Response<T> Retrieve(string id)
        {
            ICyanClient tableClient;
            dynamic[] resultEnumerable;

            tableClient.TryCreateTable(entity);
            var table = tableClient[entity];
            var result = table.Query("PK");
            // ReSharper disable once CoVariantArrayConversion
            resultEnumerable = result as dynamic[] ?? result.ToArray();
            // ReSharper disable once UseMethodAny.2
            return resultEnumerable.Count() == 0;
            
            
            
            
            return new Response<T>(HttpStatusCode.NotFound, null);
        }
    }
}
