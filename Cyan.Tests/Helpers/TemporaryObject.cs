using Microsoft.WindowsAzure.Storage.Table;

namespace Cyan.Tests.Helpers
{
    internal class TemporaryObject : TableEntity
    {
        public TemporaryObject() { }
        public TemporaryObject(string pk, string rk)
        {
            PartitionKey = pk;
            RowKey = rk;
        }

        public string Id { get; set; }
    }
}
