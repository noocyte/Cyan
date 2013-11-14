using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cyan.Fluent;
using Cyan.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using UXRisk.Lib.Common.Models;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeFluentCyan
    {
        private FluentCyan _client;
        private const string TableName = "TemporaryObject";

        [SetUp]
        public void Setup()
        {
            _client = new FluentCyan(FluentCyanHelper.GetCyanClient());
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

        [Test]
        public void ItComplainsWhenPassingInEmptyTableName()
        {
            // g
            const string tableName = "";
            var fakeClient = FluentCyanHelper.GetFakeCyanClient();
            var client = new FluentCyan(fakeClient);

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

            // w
            Action act = () => _client.FromTable(tableName);

            // t
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ItShouldDefineTheTableName()
        {
            // g
            const string tableName = "dummy";
            var fakeClient = FluentCyanHelper.GetFakeCyanClient();
            var client = new FluentCyan(fakeClient);

            // w
            client.FromTable(tableName);

            // t
            A.CallTo(() => fakeClient.TryCreateTable(tableName)).MustHaveHappened();
        }

        [Test]
        public async void ItShouldReturnNotFound_WhenQueryingForOneRecord_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<JsonObject>(HttpStatusCode.NotFound, new JsonObject());

            // w
            var actual = await _client.FromTable("dummy").RetrieveAsync("123").ConfigureAwait(false);

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void ItComplains_WhenQueryingForOneRecord_GivenInvalidID()
        {
            // g

            // w
            Func<Task<Response<JsonObject>>> func = async () => await _client.FromTable("dummy").RetrieveAsync(null).ConfigureAwait(false);

            // t
            func.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async void ItShouldReturnNotFound_WhenRetrievingAllRecords_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<IEnumerable<JsonObject>>(HttpStatusCode.NotFound, new List<JsonObject>());

            // w
            var actual = await _client.FromTable("dummy").RetrieveAllAsync().ConfigureAwait(false);

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public async void ItShouldReturnOK_WhenQueringForAllRecords_GivenRecordsExists()
        {
            // g
            var item1 = new TemporaryObject("PK", Guid.NewGuid().ToString()) { id = "item1" };
            var item2 = new TemporaryObject("PK", Guid.NewGuid().ToString()) { id = "item2" };
            var table = FluentCyanHelper.GetAzureTable<TemporaryObject>();
            table.Add(item1);
            table.Add(item2);

            var allObjects = new[] { item1, item2 };
            var expected = new Response<TemporaryObject[]>(HttpStatusCode.OK, allObjects);

            // w
            var actual = await _client.FromTable(TableName).RetrieveAllAsync().ConfigureAwait(false);

            // t
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Result.Count(), Is.EqualTo(expected.Result.Count()));
        }

        [Test]
        public async void ItShouldReturnOK_WhenQueringForOneRecord_GivenRecordExists()
        {
            // g
            var objectId = Guid.NewGuid().ToString();
            var aTimestamp = DateTime.Now;

            var json = JsonObjectFactory.CreateJsonObject(aTimestamp, objectId);
            var tableObj = new TemporaryObject("PK", objectId) { id = objectId };
            var table = FluentCyanHelper.GetAzureTable<TemporaryObject>();
            table.Add(tableObj);

            var expected = new Response<JsonObject>(HttpStatusCode.OK, json);

            // w
            var actual = await _client.FromTable(TableName).RetrieveAsync(objectId).ConfigureAwait(false);

            // t
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Result.Id, Is.EqualTo(expected.Result.Id));
            Assert.That(actual.Result.ToDictionary().ContainsKey("ETag"));
            Assert.That(actual.Result.ToDictionary().ContainsKey("Timestamp"));
        }

        [Test]
        public void ItComplains_WhenPosting_GivenInvalidJsonObject()
        {
            // g

            // w
            Func<Task<Response<JsonObject>>> func = async () => await _client.IntoTable("dummy").PostAsync(null).ConfigureAwait(false);

            // t
            func.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async void ItShouldPostOneRecord_GivenValidJsonObject()
        {
            // g 
            var json = JsonObjectFactory.CreateJsonObjectForPost();

            // w
            var response = await _client.IntoTable(TableName).PostAsync(json).ConfigureAwait(false);

            // t
            var allResponses = await _client.FromTable(TableName).RetrieveAllAsync().ConfigureAwait(false);
            allResponses.Result.Count().Should().Be(1);
            response.Status.Should().Be(HttpStatusCode.Created);
            response.Result.Id.Should().NotBeEmpty();
        }
    }
}