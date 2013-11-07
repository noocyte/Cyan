using System;
using System.Net;
using Cyan.Fluent;
using Cyan.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeFluentCyan
    {

        [Test]
        public void ItShouldReturnNotFound_WhenQueryingForOneRecord_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<TemporaryObject>(HttpStatusCode.NotFound, null);
            var client = new FluentCyan<TemporaryObject>(FluentCyanHelper.GetCyanClient());

            // w
            var actual = client.FromTable("dummy")
                               .Retrieve("123");

            // t
            actual.ShouldBeEquivalentTo(expected);
        }


        [Test]
        public void ItShouldDefineTheTableName()
        {
            // g
            const string tableName = "dummy";
            var fakeClient = FluentCyanHelper.GetFakeCyanClient();
            var client = new FluentCyan<TemporaryObject>(fakeClient);

            // w
            client.FromTable(tableName);

            // t
            A.CallTo(() => fakeClient.TryCreateTable(tableName)).MustHaveHappened();
        }

        [Test]
        public void ItComplainsWhenPassingInEmptyTableName()
        {
            // g
            const string tableName = "";
            var fakeClient = FluentCyanHelper.GetFakeCyanClient();
            var client = new FluentCyan<TemporaryObject>(fakeClient);

            // w
            Action act = () => client.FromTable(tableName);

            // t
            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ItComplainsWhenPassingInInvalidTableName()
        {
            // g
            const string tableName = "123";
            var client = new FluentCyan<TemporaryObject>(FluentCyanHelper.GetCyanClient());

            // w
            Action act = () => client.FromTable(tableName);

            // t
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ItShouldReturnOK_WhenQueringForOneRecord_GivenRecordExists()
        {
            // g
            var objectId = Guid.NewGuid().ToString();
            var expected = new Response<TemporaryObject>(HttpStatusCode.OK, new TemporaryObject("PK", objectId) { Id = objectId });
            var table = FluentCyanHelper.GetAzureTable<TemporaryObject>();
            table.Add(expected.Result);

            var client = new FluentCyan<object>(FluentCyanHelper.GetCyanClient());

            // w
            dynamic actual = client.FromTable("TemporaryObject")
                                   .Retrieve(objectId);

            // t
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Result.Id, Is.EqualTo(expected.Result.Id));
            Assert.That(actual.Result.PartitionKey, Is.EqualTo(expected.Result.PartitionKey));
            Assert.That(actual.Result.RowKey, Is.EqualTo(expected.Result.RowKey));
            Assert.That(actual.Result.ETag.Replace(":", "%3A"), Is.EqualTo(expected.Result.ETag));
            Assert.That((DateTimeOffset) (actual.Result.Timestamp), Is.EqualTo(expected.Result.Timestamp));
        }


        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
            var table = FluentCyanHelper.GetAzureTable<TemporaryObject>();
            var tobeDeleted = table.GetAll();
            foreach (var tableObject in tobeDeleted)
            {
                table.Delete(tableObject);
            }
        }


    }
}