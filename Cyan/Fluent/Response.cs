using System.Net;

namespace Cyan.Fluent
{
    public class Response<T>
    {
        public Response(HttpStatusCode status, T result)
        {
            Result = result;
            Status = status;
        }

        public HttpStatusCode Status { get; private set; }
        public T Result { get; private set; }
    }
}