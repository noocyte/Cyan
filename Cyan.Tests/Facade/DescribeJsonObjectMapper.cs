using System;
using Cyan.Fluent;
using Cyan.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeJsonObjectMapper
    {
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