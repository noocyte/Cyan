using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cyan;
using Microsoft.CSharp.RuntimeBinder;
using System.Xml.Linq;

namespace Cyan.Tests
{
    [TestClass]
    public class Entity
    {
        [TestMethod, TestCategory("Entity")]
        public void EntityFromObjectTest()
        {
            var partition = Guid.NewGuid().ToString();
            var row = Guid.NewGuid().ToString();
            var etag = Guid.NewGuid().ToString();
            var stringField = "test";
            var guidField = Guid.NewGuid();
            var intField = 1337;
            var datetimeField = DateTime.UtcNow;
            var boolField = true;
            var bytesField = new byte[] { 1, 2, 3 };

            var entity = CyanEntity.FromObject(new
                {
                    PartitionKey = partition,
                    RowKey = row,
                    ETag = etag,
                    StringField = stringField,
                    GuidField = guidField,
                    IntField = intField,
                    DatetimeField = datetimeField,
                    BoolField = boolField,
                    BytesField = bytesField
                });

            var dynamicEntity = (dynamic)entity;
            Assert.AreEqual(partition, dynamicEntity.PartitionKey);
            Assert.AreEqual(row, dynamicEntity.RowKey);
            Assert.AreEqual(etag, dynamicEntity.ETag);
            Assert.AreEqual(stringField, dynamicEntity.StringField);
            Assert.AreEqual(guidField, dynamicEntity.GuidField);
            Assert.AreEqual(intField, dynamicEntity.IntField);
            Assert.AreEqual(datetimeField, dynamicEntity.DatetimeField);
            Assert.AreEqual(boolField, dynamicEntity.BoolField);

            var actualBytes = (byte[])dynamicEntity.BytesField;
            Assert.AreEqual(bytesField.Length, actualBytes.Length);
            for (int i = 0; i < bytesField.Length; i++)
                Assert.AreEqual(bytesField[i], actualBytes[i]);
        }

        [TestMethod, TestCategory("Entity")]
        public void EntityDynamicSet()
        {
            var entity = CyanEntity.FromObject(new { Field = Guid.NewGuid().ToString() });

            var dynamicEntity = (dynamic)entity;
            dynamicEntity.Field = "expected field";

            Assert.AreEqual("expected field", entity.Fields["Field"]);
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void EntityDynamicEditPartition()
        {
            var partition = Guid.NewGuid().ToString();
            var entity = CyanEntity.FromObject(new { PartitionKey = partition });

            Assert.AreEqual(partition, entity.PartitionKey);

            var dynamicEntity = (dynamic)entity;
            Assert.AreEqual(partition, dynamicEntity.PartitionKey);
            dynamicEntity.PartitionKey = "should fail";
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void EntityDynamicEditRow()
        {
            var row = Guid.NewGuid().ToString();
            var entity = CyanEntity.FromObject(new { RowKey = row });

            Assert.AreEqual(row, entity.RowKey);

            var dynamicEntity = (dynamic)entity;
            Assert.AreEqual(row, dynamicEntity.RowKey);
            dynamicEntity.RowKey = "should fail";
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(ArgumentException))]
        public void EntityInvalidFieldType()
        {
            CyanEntity.FromObject(new { InvalidFieldType = "this is a test".Split(' ') });
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(ArgumentException))]
        public void EntityInvalidPartitionType()
        {
            CyanEntity.FromObject(new { PartitionKey = 1337 });
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(ArgumentException))]
        public void EntityInvalidRowType()
        {
            CyanEntity.FromObject(new { RowKey = 1337 });
        }

        [TestMethod, TestCategory("Entity")]
        [ExpectedException(typeof(ArgumentException))]
        public void EntityInvalidETagType()
        {
            CyanEntity.FromObject(new { ETag = 1337 });
        }

        [TestMethod, TestCategory("Entity")]
        public void EntitySerializeDeserialize()
        {
            var partition = Guid.NewGuid().ToString();
            var row = Guid.NewGuid().ToString();
            var etag = Guid.NewGuid().ToString();
            var stringField = "test";
            var guidField = Guid.NewGuid();
            var intField = 1337;
            var datetimeField = DateTime.UtcNow;
            var boolField = true;
            var bytesField = new byte[] { 1, 2, 3 };

            var entity = CyanEntity.FromObject(new
            {
                PartitionKey = partition,
                RowKey = row,
                ETag = etag,
                StringField = stringField,
                GuidField = guidField,
                IntField = intField,
                DatetimeField = datetimeField,
                BoolField = boolField,
                BytesField = bytesField
            });

            var serialized1 = entity.Serialize();
            var deserialized = CyanSerializer.DeserializeEntity(serialized1.Root);

            XDocument serialized2 = CyanSerializer.Serialize(deserialized);

            // update time should be ignored
            var updated = serialized1.Root.Elements().First(e => e.Name.LocalName == "updated").Value;
            serialized2.Root.Elements().First(e => e.Name.LocalName == "updated").Value = updated;

            Assert.AreEqual(serialized1.ToString(), serialized2.ToString());
        }

        [TestMethod, TestCategory("Entity")]
        public void GetSetUnexistingField()
        {
            var partition = Guid.NewGuid().ToString();
            var row = Guid.NewGuid().ToString();
            var etag = Guid.NewGuid().ToString();
            var stringField = "test";
            var entity = CyanEntity.FromObject(new
            {
                PartitionKey = partition,
                RowKey = row,
                ETag = etag,
                StringField = stringField
            });

            dynamic dynamicEntity = entity;

            var part = dynamicEntity.PartitionKey;
            var fields = dynamicEntity.GetDynamicMemberNames();

            var actual = dynamicEntity.Unexisting;
            Assert.AreEqual<string>(null, actual);

            // reference type
            dynamicEntity.Unexisting = "test";
            Assert.AreNotEqual(null, dynamicEntity.Unexisting);
            Assert.AreEqual("test", dynamicEntity.Unexisting);

            // set value type
            dynamicEntity.Unexisting = 5;
            Assert.AreNotEqual(null, dynamicEntity.Unexisting);
            Assert.AreEqual(5, dynamicEntity.Unexisting);

            // remove field
            dynamicEntity.Unexisting = null;
            Assert.AreEqual(null, dynamicEntity.Unexisting);
            Assert.IsFalse((dynamicEntity as CyanEntity).Fields.ContainsKey("Unexisting"));
        }
    }
}
