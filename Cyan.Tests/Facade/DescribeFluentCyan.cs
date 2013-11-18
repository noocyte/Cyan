using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
            const string invalidTableName = "123";

            // w
            Func<Task<Response<IEnumerable<JsonObject>>>> func =
                async () => await _client.FromTable(invalidTableName).GetAllAsync().ConfigureAwait(false);

            // t
            func.ShouldThrow<ArgumentException>();
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
        public async Task ItShouldReturnNotFound_WhenQueryingForOneRecord_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<JsonObject>(HttpStatusCode.NotFound, new JsonObject());

            // w
            var actual = await _client.FromTable("dummy").GetByIdAsync("123").ConfigureAwait(false);

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void ItComplains_WhenQueryingForOneRecord_GivenInvalidID()
        {
            // g

            // w
            Func<Task<Response<JsonObject>>> func =
                async () => await _client.FromTable("dummy").GetByIdAsync(null).ConfigureAwait(false);

            // t
            func.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async Task ItShouldReturnNotFound_WhenRetrievingAllRecords_GivenNoRecordsExists()
        {
            // g
            var expected = new Response<IEnumerable<JsonObject>>(HttpStatusCode.NotFound, new List<JsonObject>());

            // w
            var actual = await _client.FromTable("dummy").GetAllAsync().ConfigureAwait(false);

            // t
            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public async Task ItShouldReturnOK_WhenQueringForAllRecords_GivenRecordsExists()
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
            var actual = await _client.FromTable(TableName).GetAllAsync().ConfigureAwait(false);

            // t
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Result.Count(), Is.EqualTo(expected.Result.Count()));
        }

        [Test]
        public async Task ItShouldReturnOK_WhenQueringForOneRecord_GivenRecordExists()
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
            var actual = await _client.FromTable(TableName).GetByIdAsync(objectId).ConfigureAwait(false);

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
        public async Task ItShouldPostOneRecord_GivenValidJsonObject()
        {
            // g 
            var json = JsonObjectFactory.CreateJsonObjectForPost();

            // w
            var response = await _client.IntoTable(TableName).PostAsync(json).ConfigureAwait(false);

            // t
            var allResponses = await _client.FromTable(TableName).GetAllAsync().ConfigureAwait(false);
            allResponses.Result.Count().Should().Be(1);
            response.Status.Should().Be(HttpStatusCode.Created);
            response.Result.Id.Should().NotBeEmpty();
        }

        [Test]
        public async Task ItShouldMergeWithExitingEntity_GivenUpdatedValue()
        {
            // g 
            var json = JsonObjectFactory.CreateJsonObjectForPost(id: "one");
            var inserted = await _client.IntoTable(TableName).PostAsync(json).ConfigureAwait(false);
            var entityId = inserted.Result.Id;
            var updatedJson = JsonObjectFactory.CreateJsonObjectForPost(id: entityId, name: "newName");

            // w
            var response = await _client.IntoTable(TableName).MergeAsync(updatedJson).ConfigureAwait(false);

            // t
            var merged = await _client.FromTable(TableName).GetByIdAsync(entityId).ConfigureAwait(false);
            merged.Result["name"].Should().Be("newName");
            merged.Result["ETag"].Should().NotBe(inserted.Result["ETag"]);
        }


        [Test]
        public async Task ItShouldMergeWithExitingEntity_GivenNewField()
        {
            // g 
            const string entityId = "one";

            var json = JsonObjectFactory.CreateJsonObjectForPost(id: entityId);
            var inserted = await _client.IntoTable(TableName).PostAsync(json).ConfigureAwait(false);
            var updatedJson = await _client.FromTable(TableName).GetByIdAsync(entityId).ConfigureAwait(false);
            updatedJson.Result.Add("newField", "someValue");
            FluentCyanHelper.AddCyanSpecificStuff(updatedJson, entityId);

            // w
            var response = await _client.IntoTable(TableName).MergeAsync(updatedJson.Result).ConfigureAwait(false);

            // t
            var merged = await _client.FromTable(TableName).GetByIdAsync(entityId).ConfigureAwait(false);
            merged.Result["newField"].Should().Be("someValue");
            merged.Result["ETag"].Should().NotBe(inserted.Result["ETag"]);
        }
        

        [Test]
        public async Task ItComplains_WhenMerging_GivenOldETag()
        {
            // g 
            const string entityId = "one";

            var firstEntity = JsonObjectFactory.CreateJsonObjectForPost(id: entityId);
            var firstResponse = await _client.IntoTable(TableName).PostAsync(firstEntity).ConfigureAwait(false);

            var secondEntity = await _client.FromTable(TableName).GetByIdAsync(entityId).ConfigureAwait(false);
            secondEntity.Result.Add("newField", "newValue");
            FluentCyanHelper.AddCyanSpecificStuff(secondEntity, entityId);


            var secondResponse = await _client.IntoTable(TableName).MergeAsync(secondEntity.Result).ConfigureAwait(false);
            
            // w
            Func<Task<Response<JsonObject>>> func =
                async () => await _client.IntoTable(TableName).MergeAsync(firstResponse.Result).ConfigureAwait(false);

            // t
            func.ShouldThrow<CyanException>();
        }

    }
}