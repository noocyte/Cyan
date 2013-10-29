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
    public class EntityGroupTransactions
    {
        [TestMethod, TestCategory("Entity Group Transactions")]
        public void EGTInsertTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var transaction = table.Batch();
                for (int i = 0; i < 3; i++)
                {
                    transaction.Insert(new
                    {
                        PartitionKey = partition,
                        RowKey = i.ToString(),
                        Field = "field"
                    });
                }

                transaction.Commit();
            }
        }

        [TestMethod, TestCategory("Entity Group Transactions")]
        public void EGTInsertOrUpdateTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                // insert entities
                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = "2",
                    Field = "field"
                });

                table.Insert(new
                {
                    PartitionKey = partition,
                    RowKey = "3",
                    Field = "field"
                });


                var transaction = table.Batch();
                for (int i = 0; i < 10; i++)
                {
                    transaction.InsertOrUpdate(new
                    {
                        PartitionKey = partition,
                        RowKey = i.ToString(),
                        Field = "field"
                    });
                }

                transaction.Commit();
            }
        }

        [TestMethod, TestCategory("Entity Group Transactions")]
        public void EGTUpdateOCTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var count = 5;

                // batch insert some entities
                var entities = table.BatchInsert(Enumerable.Range(0, count).Select(row => new
                    {
                        PartitionKey = partition,
                        RowKey = row.ToString(),
                        Field = "field"
                    })).ToArray(); // force execution

                // get entities
                var fromQuery = table.Query().ToArray();
                Assert.AreEqual(count, fromQuery.Length);

                // update them
                var batchUpdate = table.Batch();
                foreach (var entity in fromQuery)
                {
                    entity.Field = "version1";
                    batchUpdate.Update(entity);
                }
                // this should succeed
                batchUpdate.Commit();

                // try to update the old version
                var batchUpdate2 = table.Batch();
                foreach (var entity in entities)
                {
                    entity.Field = "version2";
                    batchUpdate2.Update(entity);
                }
                var shouldFail = batchUpdate2.TryCommit();

                // the update on the old version should fail
                Assert.IsFalse(shouldFail);
            }
        }

        [TestMethod]
        public void EGTAppendTotalCaseTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();
                var totalRowKey = "_total";

                // insert transaction count row
                table.Insert(new
                    {
                        PartitionKey = partition,
                        RowKey = totalRowKey,
                        TransactionCount = 0
                    });

                var expectedCount = 20;
                ServicePointManager.DefaultConnectionLimit = int.MaxValue;

                // parallel append transactions
                Parallel.For(0, expectedCount, new ParallelOptions { MaxDegreeOfParallelism = expectedCount }, _ =>
                    {
                        var transactionId = Guid.NewGuid().ToString();

                        bool success = false;
                        do
                        {
                            var batch = table.Batch();
                            var totalRow = table.Query(partition, totalRowKey).First();

                            // update the transaction count
                            totalRow.TransactionCount += 1;
                            batch.Update(totalRow);

                            // appen the transaction
                            batch.Insert(new
                                {
                                    PartitionKey = partition,
                                    RowKey = transactionId,
                                    Field = "test!"
                                });

                            // try to commit the batch
                            success = batch.TryCommit();
                        } while (!success);
                    });

                var entities = table.Query().ToArray();
                var actualCount = entities.First(e => e.RowKey == totalRowKey).TransactionCount;

                var actualTransactions = entities.Where(e => e.RowKey != totalRowKey).Count();
                Assert.AreEqual(expectedCount, actualCount);
                Assert.AreEqual(expectedCount, actualTransactions);
            }
        }

        [TestMethod, TestCategory("Entity Group Transactions")]
        public void EGTDeleteTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partition = Guid.NewGuid().ToString();

                var entities = new List<CyanEntity>();
                for (int i = 0; i < 3; i++)
                {
                    entities.Add(table.Insert(new
                    {
                        PartitionKey = partition,
                        RowKey = i.ToString(),
                        Field = "field"
                    }));
                }

                var transaction = table.Batch();

                foreach (var entity in entities)
                    transaction.Delete(entity);

                transaction.Commit();
            }
        }
    }
}
