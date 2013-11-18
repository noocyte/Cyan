using System;
using System.Collections.Generic;
using Cyan.Fluent;
using Cyan.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;
using UXRisk.Lib.Common.Models;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeJsonObjectMapper
    {
        [Test]
        public void ItComplains_WhenMappingFromJsonObject_GivenInvalidJsonObject()
        {
            // g 

            // w
            Action act = (() => JsonObjectMapper.ToCyanEntity(null));

            // t
            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ItCanMapFromJsonObject()
        {
            // g 
            var json = JsonObjectFactory.CreateJsonObjectForPost();

            // w
            var actual = json.ToCyanEntity();

            // t
            actual.PartitionKey.Should().Be(json["PartitionKey"].ToString());
        }

        [Test]
        public void ItCanMapFromCyanEntity()
        {
            // g
            const string valueString = "something";
            var aTimestamp = DateTime.Now;
            var ce = new CyanEntity {ETag = valueString, Timestamp = aTimestamp};
            ce.Fields.Add("id", valueString);
            ce.Fields.Add("name", valueString);

            var expected = JsonObjectFactory.CreateJsonObject(aTimestamp);

            // w
            var json = ce.ToJsonObject();

            // t
            json.ShouldBeEquivalentTo(expected);
        }
    }
}