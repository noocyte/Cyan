using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cyan.Interfaces;
using Cyan.Policies;

namespace Cyan
{
    [DebuggerDisplay("CyanRest({accountName})")]
    public class CyanRest : ICyanRest
    {
        private readonly CyanAccount _account;

        public CyanRest(string accountName, string accountSecret, bool useSsl = false,
            CyanRetryPolicy retryPolicy = null)
        {
            UseSsl = useSsl;
            RetryPolicy = retryPolicy ?? CyanRetryPolicy.Default;

            _account = new CyanAccount(accountName, accountSecret);
        }

        /// <summary>
        /// Returns the current time formatted for the storage requests.
        /// </summary>
        private static string XMsDate
        {
            get { return DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
        }

        public bool UseSsl { get; private set; }

        public string AccountName
        {
            get { return _account.Name; }
        }

        public string AccountSecret
        {
            get { return _account.AccountSecret; }
        }

        public bool IsDevelopmentStorage
        {
            get { return AccountName == CyanClient.DevelopmentStorageAccount; }
        }

        public CyanRetryPolicy RetryPolicy { get; private set; }

        public async Task<CyanRestResponse> GetRequest(string resource, string query = null)
        {
            return await Request("GET", resource, query);
        }

        public async Task<CyanRestResponse> PostRequest(string resource, string content)
        {
            return await Request("POST", resource, content: content);
        }

        public async Task<CyanRestResponse> PutRequest(string resource, string content, string ifMatch = null)
        {
            return await Request("PUT", resource, content: content, ifMatch: ifMatch);
        }

        public async Task<CyanRestResponse> MergeRequest(string resource, string content, string ifMatch = null)
        {
            return await Request("MERGE", resource, content: content, ifMatch: ifMatch);
        }

        public async Task<CyanRestResponse> DeleteRequest(string resource, string ifMatch = null)
        {
            return await Request("DELETE", resource, ifMatch: ifMatch ?? "*");
        }

        public async Task<CyanBatchResponse> BatchRequest(string multipartBoundary, byte[] contentBytes)
        {
            var response = await GetResponse("POST",
                "$batch",
                contentType: string.Format("multipart/mixed; boundary={0}", multipartBoundary),
                contentBytes: contentBytes);

            return CyanBatchResponse.Parse(response);
        }

        public string FormatUrl(string resource, string query = null)
        {
            if (IsDevelopmentStorage)
            {
                // development storage url http://127.0.0.1:10002/devstoreaccount1/{resource}?{query}
                var url = string.Format("http://127.0.0.1:10002/{0}/{1}", AccountName, resource);
                if (!string.IsNullOrEmpty(query))
                    url = string.Join("?", url, query);

                return url;
            }
            else
            {
                // table storage {protocol}://{account}.table.core.windows.net/{resource}?{query}
                var protocol = UseSsl ? "https" : "http";

                var url = string.Format("{0}://{1}.table.core.windows.net/{2}", protocol, AccountName, resource);
                if (!string.IsNullOrEmpty(query))
                    url = string.Join("?", url, query);

                return url;
            }
        }

        private async Task<CyanRestResponse> Request(string method,
            string resource,
            string query = null,
            string contentType = null,
            string content = null,
            byte[] contentBytes = null,
            string ifMatch = null)
        {
            CyanRestResponse ret = null;

            IEnumerator<TimeSpan> retries = RetryPolicy.GetRetries().GetEnumerator();
            bool retry;
            do
            {
                retry = false;
                try
                {
                    using (var response = await GetResponse(method,
                        resource, query, contentType, content, contentBytes, ifMatch
                        ))
                        ret =  CyanRestResponse.Parse(response);
                }
                catch (Exception ex)
                {
                    if (RetryPolicy.ShouldRetry(ex) && retries.MoveNext())
                    {
                        retry = true;
                        Thread.Sleep(retries.Current);
                    }

                    if (!retry)
                        throw;
                }

                if (!retry
                    && !ret.Succeeded
                    && retries.MoveNext()
                    && RetryPolicy.ShouldRetry(CyanException.Parse(ret)))
                {
                    retry = true;
                    Thread.Sleep(retries.Current);
                }
            } while (retry);

            return ret;
        }

        private async Task<HttpWebResponse> GetResponse(string method,
            string resource,
            string query = null,
            string contentType = null,
            string content = null,
            byte[] contentBytes = null,
            string ifMatch = null)
        {
            var url = FormatUrl(resource, query);

            if (contentBytes == null)
            {
                // encode request content
                contentBytes = !string.IsNullOrEmpty(content) ? Encoding.UTF8.GetBytes(content) : null;
            }

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = method;

            // required headers
            request.ContentType = contentType ?? "application/atom+xml";
            request.Headers.Add("DataServiceVersion", "2.0;NetFx");
            request.Headers.Add("MaxDataServiceVersion", "2.0;NetFx");
            request.Headers.Add("x-ms-date", XMsDate);
            request.Headers.Add("x-ms-version", "2011-08-18");

            if (!string.IsNullOrEmpty(ifMatch))
                request.Headers.Add("If-Match", ifMatch);

            // sign the request
            _account.Sign(request);

            try
            {
                if (contentBytes != null)
                {
                    // we have some content to send
                    request.ContentLength = contentBytes.Length;

                    using (var requestStream = request.GetRequestStream())
                        requestStream.Write(contentBytes, 0, contentBytes.Length);
                }

                var resp = request.GetResponse();
                //var resp = await request.GetResponseAsync();
                return (HttpWebResponse) resp;
            }
            catch (WebException webEx)
            {
                // if ProtocolError (ie connection problems) throw
                // in this case I should probably implement a retry policy
                if (webEx.Status != WebExceptionStatus.ProtocolError)
                    throw;

                // we have a response from the service
                return (HttpWebResponse) webEx.Response;
            }
        }
    }
}