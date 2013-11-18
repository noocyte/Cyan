using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Cyan.Interfaces;

namespace Cyan
{
    [DebuggerDisplay("CyanTable({TableName})")]
    public class CyanTable : ICyanTable
    {
        public CyanTable(string tableName, CyanClient client)
        {
            TableName = tableName;
            RestClient = client.RestClient;
        }

        public ICyanRest RestClient { get; set; }

        public string TableName { get; private set; }

        public string FormatResource(string partitionKey, string rowKey)
        {
            return CyanUtilities.FormatResource(TableName, partitionKey, rowKey);
        }

        #region Operations on Entities

        public async Task<IEnumerable<CyanEntity>> Query(string partition = null,
            string row = null,
            string filter = null,
            int top = 0,
            bool disableContinuation = false,
            params string[] fields)
        {
            var single = !string.IsNullOrEmpty(partition) && !string.IsNullOrEmpty(row);

            var resource = FormatResource(partition, row);

            var returned = 0;
            bool hasContinuation;
            string nextPartition = null;
            string nextRow = null;
            var entityList = new List<CyanEntity>();

            do
            {
                var query = CyanUtilities.FormatQuery(partition, row, filter, top, fields, nextPartition, nextRow);

                var response = await RestClient.GetRequest(resource, query).ConfigureAwait(false);

                if (single)
                {
                    // should not throw NotFound, should return empty
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        break;

                    response.ThrowIfFailed();
                    entityList.Add(CyanSerializer.DeserializeEntity(response.ResponseBody.Root));
                }

                response.ThrowIfFailed();

                // just one | because both statements must be executed everytime
                hasContinuation = response.Headers.TryGetValue("x-ms-continuation-NextPartitionKey", out nextPartition)
                                  | response.Headers.TryGetValue("x-ms-continuation-NextRowKey", out nextRow);

                var entities = CyanSerializer.DeserializeEntities(response.ResponseBody.Root);
                foreach (var entity in entities)
                {
                    entityList.Add(entity);
                    if (top > 0 && ++returned >= top)
                        break;
                }
            } while (!disableContinuation // continuation has not been disabled
                     && hasContinuation // the response has a valid continuation
                     && !(top > 0 && returned >= top));
            // if there is a top argument and we didn't return enough entities

            return entityList;
        }

        public async Task Delete(CyanEntity cyanEntity)
        {
            var resource = FormatResource(cyanEntity.PartitionKey, cyanEntity.RowKey);
            var response = await RestClient.DeleteRequest(resource, cyanEntity.ETag).ConfigureAwait(false);
            response.ThrowIfFailed();
        }


        public async Task<CyanEntity> Merge(CyanEntity cyanEntity, bool unconditionalUpdate = false)
        {
            var partition = cyanEntity.PartitionKey;
            var row = cyanEntity.RowKey;
            var eTag = cyanEntity.ETag;

            var document = cyanEntity.Serialize();
            var resource = FormatResource(partition, row);

            var response =
                await RestClient.MergeRequest(resource, document.ToString(), unconditionalUpdate ? "*" : eTag);

            string newETag;
            if (response.Headers.TryGetValue("ETag", out newETag))
                cyanEntity.ETag = HttpUtility.UrlDecode(newETag);

            response.ThrowIfFailed();

            return cyanEntity;
        }

        public async Task<CyanEntity> Insert(CyanEntity cyanEntity)
        {
            var document = cyanEntity.Serialize();
            var response = await RestClient.PostRequest(TableName, document.ToString()).ConfigureAwait(false);
            response.ThrowIfFailed();
            return CyanSerializer.DeserializeEntity(response.ResponseBody.Root);
        }

        #endregion
    }
}