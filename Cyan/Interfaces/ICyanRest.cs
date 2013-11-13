using System.Threading.Tasks;
using Cyan.Policies;

namespace Cyan.Interfaces
{
    public interface ICyanRest
    {
        bool UseSsl { get; }
        string AccountName { get; }
        bool IsDevelopmentStorage { get; }
        CyanRetryPolicy RetryPolicy { get; }
        Task<CyanRestResponse> GetRequest(string resource, string query = null);
        Task<CyanRestResponse> PostRequest(string resource, string content);
        Task<CyanRestResponse> PutRequest(string resource, string content, string ifMatch = null);
        Task<CyanRestResponse> MergeRequest(string resource, string content, string ifMatch = null);
        Task<CyanRestResponse> DeleteRequest(string resource, string ifMatch = null);
        Task<CyanBatchResponse> BatchRequest(string multipartBoundary, byte[] contentBytes);
        string FormatUrl(string resource, string query = null);
    }
}