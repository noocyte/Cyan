using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Cyan.Tests.Facade
{
    [TestFixture]
    public class DescribeCyanEntity
    {
        private static IEnumerable<KeyValuePair<string, object>> CreateSimpleInput(object value)
        {
            var input = new List<KeyValuePair<string, object>>()
            {
                new KeyValuePair<string, object>("one", value),
                new KeyValuePair<string, object>("TWO", value)
            };
            return input;
        }
       

        [Test]
        public void ItShouldLowerCaseAllKeys()
        {
            // g
            var input = CreateSimpleInput("somevalue");

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields.ContainsKey("two").Should().BeTrue();
        }

        [Test]
        public void ItShouldNotSerializeDatetimeToString()
        {
            // g
            var datetime = DateTime.UtcNow;
            var input = CreateSimpleInput(datetime);

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].Should().Be(datetime);
        }

        [Test]
        public void ItShouldNotSerializeIntToString()
        {
            // g
            var input = CreateSimpleInput(1001);

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].Should().Be(1001);
        }

        [Test]
        public void ItShouldNotSerializeStringToString()
        {
            // g
            var input = CreateSimpleInput("one, two");

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("one, two");
        }

        [Test]
        public void ItShouldSerializeListOfStringToString()
        {
            // g
            var input = CreateSimpleInput(new List<string>() {"one", "two"});

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("[\"one\",\"two\"]");
        }

        [Test]
        public void ItShouldSerializeObjectArrayToString()
        {
            // g
            var input = CreateSimpleInput(new object[] {"one", "two"});

            // w
            var actual = CyanEntity.FromEnumerable(input);

            // t
            actual.Fields["one"].ToString().Should().Be("[\"one\",\"two\"]");
        }
    }
}