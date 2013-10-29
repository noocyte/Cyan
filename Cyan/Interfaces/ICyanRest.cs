using Cyan.Policies;

namespace Cyan.Interfaces
{
    public interface ICyanRest
    {
        bool UseSsl { get; }
        string AccountName { get; }
        bool IsDevelopmentStorage { get; }
        CyanRetryPolicy RetryPolicy { get; }
        CyanRestResponse GetRequest(string resource, string query = null);
        CyanRestResponse PostRequest(string resource, string content);
        CyanRestResponse PutRequest(string resource, string content, string ifMatch = null);
        CyanRestResponse MergeRequest(string resource, string content, string ifMatch = null);
        CyanRestResponse DeleteRequest(string resource, string ifMatch = null);
        CyanBatchResponse BatchRequest(string multipartBoundary, byte[] contentBytes);
        string FormatUrl(string resource, string query = null);
    }
}