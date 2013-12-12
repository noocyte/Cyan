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

// ReSharper disable once InconsistentNaming
        public string id { get; set; }
        public string SomeValue { get; set; }
        public bool sys_deleted { get; set; }
    }
}
