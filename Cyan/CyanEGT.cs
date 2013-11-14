using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Cyan.Interfaces;

namespace Cyan
{
    public class CyanEGT : ICyanEGT
    {
        private readonly HashSet<string> _modifiedRows = new HashSet<string>();
        private readonly List<EntityOperation> _operations = new List<EntityOperation>();
        private readonly ICyanTable _table;
        private string _partitionKey;

        internal CyanEGT(ICyanTable table)
        {
            _table = table;
        }

        public dynamic Insert(object entity)
        {
            var cyanEntity = CyanEntity.FromObject(entity);

            AddOperation(cyanEntity,
                "POST",
                _table.TableName);

            return cyanEntity;
        }

        public void InsertOrUpdate(object entity)
        {
            var cyanEntity = CyanEntity.FromObject(entity);

            AddOperation(cyanEntity,
                "PUT",
                _table.FormatResource(cyanEntity.PartitionKey, cyanEntity.RowKey));
        }

        public void Update(object entity, bool unconditionalUpdate = false)
        {
            var cyanEntity = CyanEntity.FromObject(entity);

            AddOperation(cyanEntity,
                "PUT",
                _table.FormatResource(cyanEntity.PartitionKey, cyanEntity.RowKey),
                Tuple.Create("If-Match", unconditionalUpdate ? "*" : cyanEntity.ETag));
        }

        public void Merge(object entity, bool unconditionalUpdate = false)
        {
            var cyanEntity = CyanEntity.FromObject(entity);

            AddOperation(cyanEntity,
                "MERGE",
                _table.FormatResource(cyanEntity.PartitionKey, cyanEntity.RowKey),
                Tuple.Create("If-Match", unconditionalUpdate ? "*" : cyanEntity.ETag));
        }

        public void Delete(object entity, bool unconditionalUpdate = false)
        {
            var cyanEntity = CyanEntity.FromObject(entity);

            Delete(cyanEntity.PartitionKey, cyanEntity.RowKey, unconditionalUpdate ? null : cyanEntity.ETag);
        }

        public void Delete(string partitionKey, string rowKey, string eTag = null)
        {
            AddOperation(partitionKey,
                rowKey,
                "DELETE",
                _table.FormatResource(partitionKey, rowKey),
                Tuple.Create("If-Match", eTag ?? "*"));
        }

        public async void Commit()
        {
            var batchBoundary = string.Format("batch_{0}", Guid.NewGuid());
            var requestBody = EncodeBatchRequestBody(batchBoundary);

            var response = await _table.RestClient.BatchRequest(batchBoundary, requestBody).ConfigureAwait(false);

            response.ThrowIfFailed();

            foreach (var operationResponse in response.Responses)
            {
                var index = int.Parse(operationResponse.Key);
                EntityOperation op = _operations[index];

                string eTagHeader;
                // update entity etag
                if (operationResponse.Value.Headers.TryGetValue("ETag", out eTagHeader))
                    op.UpdateEntityETag(HttpUtility.UrlDecode(eTagHeader));
            }
        }

        public async Task<bool> TryCommit()
        {
            if (_operations.Count == 0)
                return true;

            var batchBoundary = string.Format("batch_{0}", Guid.NewGuid());
            var requestBody = EncodeBatchRequestBody(batchBoundary);

            var response = await _table.RestClient.BatchRequest(batchBoundary, requestBody).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Accepted)
                response.ThrowIfFailed();

            var failedPrecondition = response.Responses.Values.FirstOrDefault(r => r.PreconditionFailed);
            if (failedPrecondition != null)
                return false;

            response.ThrowIfFailed();

            foreach (var operationResponse in response.Responses)
            {
                var index = int.Parse(operationResponse.Key);
                EntityOperation op = _operations[index];

                string eTagHeader;
                // update entity etag
                if (operationResponse.Value.Headers.TryGetValue("ETag", out eTagHeader))
                    op.UpdateEntityETag(eTagHeader);
            }

            return true;
        }

        private byte[] EncodeBatchRequestBody(string batchBoundary)
        {
            var changesetBoundary = string.Format("changeset_{0}", Guid.NewGuid());

            byte[] contentBytes;

            using (var contentStream = new EGTRequestStream())
            {
                // write batch boundary
                contentStream.WriteBoundary(batchBoundary);
                // write batch Content-Type header
                contentStream.WriteHeader("Content-Type",
                    string.Format("multipart/mixed; boundary={0}", changesetBoundary));
                // blank line after headers
                contentStream.WriteLine();

                int index = 0;
                foreach (var operation in _operations)
                {
                    // each changeset
                    // write changeset begin boundary
                    contentStream.WriteBoundary(changesetBoundary);

                    // required headers
                    contentStream.WriteHeader("Content-Type", "application/http");
                    contentStream.WriteHeader("Content-Transfer-Encoding", "binary");
                    contentStream.WriteLine();

                    // write changeset payload
                    operation.Write(contentStream, _table.RestClient, index++.ToString(CultureInfo.InvariantCulture));
                }

                // write changeset and batch end boundaries
                contentStream.WriteEndBoundary(changesetBoundary);
                contentStream.WriteEndBoundary(batchBoundary);

                contentBytes = contentStream.ToArray();
            }

            //var debug = Encoding.UTF8.GetString(contentBytes);

            return contentBytes;
        }

        private void ValidateEntity(string partitionKey, string rowKey)
        {
            if (partitionKey == null)
                throw new ArgumentNullException("partitionKey");
            if (rowKey == null)
                throw new ArgumentNullException("rowKey");

            if (_partitionKey == null)
            {
                _partitionKey = partitionKey;
            }
            else
            {
                if (_partitionKey != partitionKey)
                    throw new ArgumentException("Invalid partition key.", "partitionKey");
            }

            if (_modifiedRows.Contains(rowKey))
            {
                throw new NotSupportedException(
                    "Multiple operations on the same entity are not supported in the same batch.");
            }

            _modifiedRows.Add(rowKey);
        }

        private void AddOperation(string partitionKey,
            string rowKey,
            string method,
            string resource,
            params Tuple<string, string>[] headers)
        {
            ValidateEntity(partitionKey, rowKey);

            _operations.Add(EntityOperation.CreateOperation(null, method, resource, headers));
        }

        private void AddOperation(CyanEntity entity,
            string method,
            string resource,
            params Tuple<string, string>[] headers)
        {
            ValidateEntity(entity.PartitionKey, entity.RowKey);

            _operations.Add(EntityOperation.CreateOperation(entity, method, resource, headers));
        }

        private class EntityOperation
        {
            private CyanEntity _entity;
            private IEnumerable<Tuple<string, string>> _headers;
            private string _method;
            private string _resource;

            private EntityOperation()
            {
            }

            public static EntityOperation CreateOperation(CyanEntity entity,
                string method,
                string resource,
                params Tuple<string, string>[] headers)
            {
                if (string.IsNullOrEmpty(method))
                    throw new ArgumentNullException("method");
                if (string.IsNullOrEmpty(resource))
                    throw new ArgumentNullException("resource");

                if (headers == null)
                    headers = new Tuple<string, string>[0];

                var ret = new EntityOperation
                {
                    _entity = entity,
                    _method = method,
                    _resource = resource,
                    _headers = headers
                };

                return ret;
            }

            public void UpdateEntityETag(string eTag)
            {
                if ((_method == "POST" || _method == "PUT" || _method == "MERGE") && _entity != null)
                    _entity.ETag = eTag;
            }

            public void Write(EGTRequestStream requestStream, ICyanRest restClient, string contentId)
            {
                byte[] contentBytes = null;
                if (_entity != null)
                {
                    var content = _entity.Serialize();
                    contentBytes = Encoding.UTF8.GetBytes(content.ToString());
                }

                var finalHeaders = new List<Tuple<string, string>> {Tuple.Create("Content-ID", contentId)};

                if (contentBytes != null && contentBytes.Length > 0)
                {
                    finalHeaders.Add(Tuple.Create("Content-Type", "application/atom+xml;type=entry"));
                    finalHeaders.Add(Tuple.Create("Content-Length",
                        contentBytes.Length.ToString(CultureInfo.InvariantCulture)));
                }

                if (_headers != null)
                    finalHeaders.AddRange(_headers);

                // write status line
                requestStream.WriteLine("{0} {1} {2}", _method, restClient.FormatUrl(_resource), "HTTP/1.1");

                // write headers
                foreach (var header in finalHeaders)
                    requestStream.WriteHeader(header.Item1, header.Item2);
                requestStream.WriteLine();

                // write content
                if (contentBytes != null)
                {
                    requestStream.Write(contentBytes, 0, contentBytes.Length);
                    requestStream.WriteLine();
                }
            }
        }
    }
}