using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cyan;
using System.Threading.Tasks;
using System.Net;

namespace Cyan.Tests
{
    [TestClass]
    public class EntityOperations
    {
        [TestMethod]
        public void InsertTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var item = table.Query(partition, partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);
            }
        }

        [TestMethod]
        public void TryInsertTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                dynamic inserted;
                // should success
                Assert.IsTrue(table.TryInsert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                }, out inserted));

                Assert.AreEqual(partition, inserted.PartitionKey);
                Assert.AreEqual(partition, inserted.RowKey);
                Assert.AreEqual("My field", inserted.Field);

                // should fail because duplicate
                Assert.IsFalse(table.TryInsert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field2"
                }));

                var item = table.Query(partition, partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);
            }
        }

        [TestMethod]
        public void DeleteTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var item = table.Query(partition, partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);

                table.Delete(item);

                var shouldNull = table.Query(partition, partition).FirstOrDefault();

                Assert.IsNull(shouldNull);
            }
        }

        [TestMethod]
        public void QueryExactTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var item = table.Query(partition, partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);
            }
        }

        [TestMethod]
        public void QueryPartitionTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var item = table.Query(partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);
            }
        }

        [TestMethod]
        public void QueryRowTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var item = table.Query(row: partition).First();

                Assert.AreEqual(partition, item.PartitionKey);
                Assert.AreEqual(partition, item.RowKey);
                Assert.AreEqual("My field", item.Field);
            }
        }

        [TestMethod]
        public void QueryFilterTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var expectedGuid = Guid.NewGuid();

                table.Insert(new
                {
                    PartitionKey = Guid.NewGuid().ToString(),
                    RowKey = Guid.NewGuid().ToString(),
                    String = "string1",
                    Boolean = true,
                    DateTime = new DateTime(1984, 10, 16, 0, 0, 0, DateTimeKind.Utc),
                    Guid = expectedGuid,
                    Number = 1337
                });

                table.Insert(new
                {
                    PartitionKey = Guid.NewGuid().ToString(),
                    RowKey = Guid.NewGuid().ToString(),
                    String = "string2",
                    Boolean = false,
                    DateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Guid = Guid.NewGuid(),
                    Number = 42
                });

                // filtering string
                var itemString = table.Query(filter: "String eq 'string1'").First();
                Assert.AreEqual("string1", itemString.String);

                // filtering bool
                var itemBool = table.Query(filter: "Boolean eq true").First();
                Assert.AreEqual(true, itemBool.Boolean);

                // filtering DateTime
                var itemDateTime = table.Query(filter: "DateTime eq datetime'1984-10-16T00:00:00Z'").First();
                Assert.AreEqual(new DateTime(1984, 10, 16, 0, 0, 0, DateTimeKind.Utc), itemDateTime.DateTime);

                // filtering Guid
                var itemGuid = table.Query(filter: string.Format("Guid eq guid'{0}'", expectedGuid)).First();
                Assert.AreEqual(expectedGuid, itemGuid.Guid);

                // filtering int
                var itemInt = table.Query(filter: "Number eq 1337").First();
                Assert.AreEqual(1337, itemInt.Number);
            }
        }

        [TestMethod]
        public void QueryScanContinuationTest()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            using (var table = TestUtilities.CreateTestTable())
            {
                var partitionCount = 3;
                var itemsPerPartition = 1000;

                var partitions = Enumerable.Range(0, partitionCount).Select(_ => Guid.NewGuid().ToString()).ToArray();

                var toInsert = partitions.SelectMany(p => Enumerable
                    .Range(0, itemsPerPartition)
                    .Select(i => new
                    {
                        PartitionKey = p,
                        RowKey = i.ToString(),
                        Field = "My field"
                    }));

                table.BatchInsert(toInsert).ToArray();

                var items = table.Query().ToArray();

                Assert.AreEqual(itemsPerPartition * partitionCount, items.Length);

                var partitioned = items
                    .GroupBy(i => (string)i.PartitionKey)
                    .ToDictionary(g => g.Key, g => g.ToArray());

                foreach (var part in partitions)
                {
                    Assert.IsTrue(partitioned.ContainsKey(part));
                    var partition = partitioned[part];
                    for (int i = 0; i < itemsPerPartition; i++)
                    {
                        var index = i.ToString();
                        Assert.IsTrue(partition.Any(item => item.RowKey == index));
                    }
                }
            }
        }

        [TestMethod]
        public void QueryProjectionTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                // insert an entity
                table.Insert(new
                    {
                        PartitionKey = Guid.NewGuid().ToString(),
                        RowKey = Guid.NewGuid().ToString(),
                        Field1 = "some",
                        Field2 = 42
                    });

                var field1 = table.Query(fields: new[] { "Field1" }).First();
                Assert.AreEqual("some", field1.Field1);
                Assert.AreEqual(null, field1.Field2);

                var field2 = table.Query(fields: new[] { "Field2" }).First();
                Assert.AreEqual(null, field2.Field1);
                Assert.AreEqual(42, field2.Field2);

                var field12 = table.Query(fields: new[] { "Field1", "Field2" }).First();
                Assert.AreEqual("some", field12.Field1);
                Assert.AreEqual(42, field12.Field2);

                var field3 = table.Query(fields: new[] { "Field3" }).First();
                Assert.AreEqual(null, field3.Field1);
                Assert.AreEqual(null, field3.Field2);
                Assert.AreEqual(null, field3.Field3);
            }
        }

        [TestMethod]
        public void QueryNotFoundTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var count = table.Query(partition, partition).Count();

                Assert.AreEqual(0, count);
            }
        }

        [TestMethod]
        public void TryUpdateOCTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                Assert.IsTrue(table.TryUpdate(fromQuery));

                entity.Field = "wrong value";
                // this should fail
                Assert.IsFalse(table.TryUpdate(entity));
            }
        }

        [TestMethod]
        public void UpdateTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                var fromQuery1 = table.Query(partition, partition).First();
                Assert.AreEqual("My field", fromQuery1.Field);

                entity.Field = "Nice!";

                table.Update(entity);

                var fromQuery2 = table.Query(partition, partition).First();
                Assert.AreEqual("Nice!", fromQuery2.Field);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CyanException))]
        public void UpdateOCFailureTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                table.Update(fromQuery);

                entity.Field = "wrong value";
                // this should fail
                table.Update(entity);
            }
        }

        [TestMethod]
        public void UpdateUnconditionalTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                table.Update(fromQuery);
                // check its updated
                var fromQuery2 = table.Query(partition, partition).First();
                Assert.AreEqual("new value", fromQuery2.Field);

                entity.Field = "newer value";
                // this should succeed
                table.Update(entity, true);
                var fromQuery3 = table.Query(partition, partition).First();
                Assert.AreEqual("newer value", fromQuery3.Field);
            }
        }

        [TestMethod]
        public void TryMergeOCTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                Assert.IsTrue(table.TryMerge(fromQuery));

                entity.Field = "wrong value";
                // this should fail
                Assert.IsFalse(table.TryMerge(entity));
            }
        }

        [TestMethod]
        public void MergeTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field",
                });

                var fromQuery1 = table.Query(partition, partition).First();
                Assert.AreEqual("My field", fromQuery1.Field);

                entity.Field = "Nice!";

                table.Merge(entity);

                // get the actual entity
                var fromQuery2 = table.Query(partition, partition).First();
                // check the updated field
                Assert.AreEqual("Nice!", fromQuery2.Field);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CyanException))]
        public void MergeOCFailureTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                table.Merge(fromQuery);

                entity.Field = "wrong value";
                // this should fail
                table.Merge(entity);
            }
        }

        [TestMethod]
        public void MergeUnconditionalTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field"
                });

                // in the meanwhile get the element and update it
                var fromQuery = table.Query(partition, partition).First();

                fromQuery.Field = "new value";
                // this should succeed
                table.Merge(fromQuery);
                // check its updated
                var fromQuery2 = table.Query(partition, partition).First();
                Assert.AreEqual("new value", fromQuery2.Field);

                entity.Field = "newer value";
                // this should succeed
                table.Merge(entity, true);

                // check success
                var fromQuery3 = table.Query(partition, partition).First();
                Assert.AreEqual("newer value", fromQuery3.Field);
            }
        }

        [TestMethod]
        public void MergeFilteringFields()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var entity = table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = partition,
                    Field = "My field",
                    NotMerged = "My other field"
                });

                var fromQuery1 = table.Query(partition, partition).First();
                Assert.AreEqual("My field", fromQuery1.Field);
                Assert.AreEqual("My other field", fromQuery1.NotMerged);

                entity.Field = "Nice!";

                table.Merge(entity, false, "Field");

                var fromQuery2 = table.Query(partition, partition).First();
                Assert.AreEqual("Nice!", fromQuery2.Field);

                // this should not have changed
                Assert.AreEqual("My other field", fromQuery2.NotMerged);
            }
        }

        [TestMethod]
        public void InsertOrUpdateTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partitionUpdate = Guid.NewGuid().ToString();
                var partitionInsert = Guid.NewGuid().ToString();

                table.Insert(new
                {
                    PartitionKey = partitionUpdate,
                    RowKey = "row",
                    Field = "oldvalue"
                });

                var entityToUpdate = table.Query(partitionUpdate, "row").FirstOrDefault();
                var entityToInsert = table.Query(partitionInsert, "row").FirstOrDefault();
                Assert.AreEqual("oldvalue", entityToUpdate.Field);
                Assert.AreEqual(null, entityToInsert);

                CyanEntity updated = table.InsertOrUpdate(new
                {
                    PartitionKey = partitionUpdate,
                    RowKey = "row",
                    Field = "newvalue"
                });
                CyanEntity inserted = table.InsertOrUpdate(new
                {
                    PartitionKey = partitionInsert,
                    RowKey = "row",
                    Field = "newvalue"
                });

                var entityUpdated = table.Query(partitionUpdate, "row").FirstOrDefault();
                var entityInserted = table.Query(partitionInsert, "row").FirstOrDefault();
                Assert.AreEqual("newvalue", entityUpdated.Field);
                Assert.AreEqual("newvalue", entityInserted.Field);

                // check etags
                Assert.AreEqual(updated.ETag, ((CyanEntity)entityUpdated).ETag);
                Assert.AreEqual(inserted.ETag, ((CyanEntity)entityInserted).ETag);
            }
        }

        [TestMethod]
        public void InsertOrMergeTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partitionMerge = Guid.NewGuid().ToString();
                var partitionInsert = Guid.NewGuid().ToString();

                table.Insert(new
                {
                    PartitionKey = partitionMerge,
                    RowKey = "row",
                    Field = "oldvalue",
                    DontMergeMe = "oldvalue"
                });

                var entityToMerge = table.Query(partitionMerge, "row").FirstOrDefault();
                var entityToInsert = table.Query(partitionInsert, "row").FirstOrDefault();
                Assert.AreEqual("oldvalue", entityToMerge.Field);
                Assert.AreEqual(null, entityToInsert);

                CyanEntity merged = table.InsertOrMerge(new
                {
                    PartitionKey = partitionMerge,
                    RowKey = "row",
                    Field = "newvalue",
                    DontMergeMe = "newvalue"
                }, "Field");
                CyanEntity inserted = table.InsertOrMerge(new
                {
                    PartitionKey = partitionInsert,
                    RowKey = "row",
                    Field = "newvalue",
                    DontMergeMe = "newvalue"
                }, "Field");

                var entityMerged = table.Query(partitionMerge, "row").FirstOrDefault();
                var entityInserted = table.Query(partitionInsert, "row").FirstOrDefault();
                Assert.AreEqual("newvalue", entityMerged.Field);
                Assert.AreEqual("newvalue", entityInserted.Field);

                // check Merge behaviour
                Assert.AreEqual("oldvalue", entityMerged.DontMergeMe);
                Assert.AreEqual(null, entityInserted.DontMergeMe);

                // check etags
                Assert.AreEqual(merged.ETag, ((CyanEntity)entityMerged).ETag);
                Assert.AreEqual(inserted.ETag, ((CyanEntity)entityInserted).ETag);
            }
        }
    }
}
