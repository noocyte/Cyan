using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeCyanEntity
    {
        private static IEnumerable<KeyValuePair<string, object>> CreateInput(object value)
        {
            var input = new List<KeyValuePair<string, object>>()
            {
                new KeyValuePair<string, object>("one", value)
            };
            return input;
        }

        [Test]
        public void ItShouldNotSerializeDatetimeToString()
        {
            // g
            var datetime = DateTime.UtcNow;
            var input = CreateInput(datetime);

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].Should().Be(datetime);
        }

        [Test]
        public void ItShouldNotSerializeIntToString()
        {
            // g
            var input = CreateInput(1001);

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].Should().Be(1001);
        }

        [Test]
        public void ItShouldNotSerializeStringToString()
        {
            // g
            var input = CreateInput("one, two");

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("one, two");
        }

        [Test]
        public void ItShouldSerializeListOfStringToString()
        {
            // g
            var input = CreateInput(new List<string>() {"one", "two"});

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("[\"one\",\"two\"]");
        }

        [Test]
        public void ItShouldSerializeObjectArrayToString()
        {
            // g
            var input = CreateInput(new object[] {"one", "two"});

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("[\"one\",\"two\"]");
        }
    }
}