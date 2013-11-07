using System;
using System.Configuration;
using System.Net;
using Cyan.Fluent;
using FluentAssertions;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using NUnit.Framework;
using UXRisk.Lib.Common.Services;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeFluentCyan
    {
        private AzureTable<TemporaryObject> TableClient;

        [Test]
        public void ItShouldReturnNotFound_WhenQueryingForOneRecord_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<object>(HttpStatusCode.NotFound, null);
            var client = new FluentCyan<object>();

            // w
            var actual = client.Retrieve("123");

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        [TearDown]
        public void Teardown()
        {
            var tobeDeleted = TableClient.GetAll();
            foreach (var tableObject in tobeDeleted)
            {
                TableClient.Delete(tableObject);
            }  
        }

        [SetUp]
        public void Setup()
        {
            var credentials = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            TableClient = new AzureTable<TemporaryObject>(credentials); 
        }

        [Test]
        public void ItShouldReturnFound_WhenQueringForOneRecord_GivenRecordExists()
        {
            // g
            var objectId = Guid.NewGuid().ToString();
            var expected = new Response<TemporaryObject>(HttpStatusCode.OK, new TemporaryObject("PK", objectId) { Id = objectId });
            TableClient.Add(expected.Result);

            var client = new FluentCyan<object>();

            // w
            var actual = client.Retrieve(objectId);

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        class TemporaryObject : TableEntity
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
}