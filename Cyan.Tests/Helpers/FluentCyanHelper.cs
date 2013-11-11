using System.Configuration;
using Cyan.Interfaces;
using Cyan.Policies;
using FakeItEasy;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using UXRisk.Lib.Common.Interfaces.Services;
using UXRisk.Lib.Common.Services;

namespace Cyan.Tests.Helpers
{
    public static class FluentCyanHelper
    {
        private static ICyanClient _cyanClient;

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
            if (_cyanClient == null)
                _cyanClient = new CyanClient(GetAccount().Credentials.AccountName, GetAccount().Credentials.ExportBase64EncodedKey(),
                    true, CyanRetryPolicy.Default);

            return _cyanClient;
        }

        internal static ICyanClient GetFakeCyanClient()
        {
            return A.Fake<ICyanClient>();
        }
    }
}
