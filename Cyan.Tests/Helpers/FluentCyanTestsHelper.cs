using System.Configuration;
using System.Threading.Tasks;
using Cyan.Fluent;
using Cyan.Interfaces;
using Cyan.Policies;
using FakeItEasy;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using UXRisk.Lib.Common.Interfaces.Services;
using UXRisk.Lib.Common.Models;
using UXRisk.Lib.Common.Services;

namespace Cyan.Tests.Helpers
{
    public static class FluentCyanTestsHelper
    {
        internal static ICyanClient CyanClient;

        internal static void AddCyanSpecificStuff(Response<JsonObject> updatedJson, string entityId)
        {
            updatedJson.Result.Add("RowKey", entityId);
            updatedJson.Result.Add("PartitionKey", "PK");
        }


        internal static CloudStorageAccount GetAccount()
        {
            return CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        }

        internal static IAzureTable<T> GetAzureTable<T>() where T : ITableEntity, new()
        {
            return new AzureTable<T>(GetAccount());
        }

        internal static ICyanClient GetCyanClient()
        {
            if (CyanClient == null)
                CyanClient = new CyanClient(GetAccount().Credentials.AccountName, GetAccount().Credentials.ExportBase64EncodedKey(),
                    true, CyanRetryPolicy.Default);

            return CyanClient;
        }

        internal static ICyanClient GetFakeCyanClient()
        {
            return A.Fake<ICyanClient>();
        }

        internal static async Task<Response<JsonObject>> GivenOldETag(FluentCyan client, string tableName)
        {
            const string entityId = "one";

            var firstEntity = JsonObjectFactory.CreateJsonObjectForPost(id: entityId);
            var firstResponse = await client.IntoTable(tableName).PostAsync(firstEntity).ConfigureAwait(false);

            var secondEntity = await client.FromTable(tableName).GetByIdAsync(entityId).ConfigureAwait(false);
            secondEntity.Result.Add("newField", "newValue");
            FluentCyanTestsHelper.AddCyanSpecificStuff(secondEntity, entityId);

            var secondResponse = await client.IntoTable(tableName).MergeAsync(secondEntity.Result).ConfigureAwait(false);
            return firstResponse;
        }
    }
}
