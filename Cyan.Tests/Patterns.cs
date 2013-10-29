using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cyan;
using System.Threading.Tasks;
using System.Threading;

namespace Cyan.Tests
{
    [TestClass]
    public class Patterns
    {
        [TestMethod]
        public void InsertOrUpdateTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                int expectedTotal = 0;

                var partition = Guid.NewGuid().ToString();
                var row = Guid.NewGuid().ToString();

                Parallel.ForEach(GetRandomSequence().Take(20),
                    i =>
                    {
                        Interlocked.Add(ref expectedTotal, i);
                        table.InsertOrUpdate(partition,
                            row,
                            () => new { PartitionKey = partition, RowKey = row, Number = i },
                            e => e.Number += i);
                    });

                var actualTotal = table.Query(partition, row).First().Number;
                Assert.AreEqual(expectedTotal, actualTotal);
            }
        }

        [TestMethod]
        public void InsertOrMergeTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                int expectedTotal = 0;

                var partition = Guid.NewGuid().ToString();
                var row = Guid.NewGuid().ToString();

                Parallel.ForEach(GetRandomSequence().Take(20),
                    i =>
                    {
                        Interlocked.Add(ref expectedTotal, i);
                        table.InsertOrMerge(partition,
                            row,
                            () => new { PartitionKey = partition, RowKey = row, Number = i, Number2 = i, Untouched = i },
                            e =>
                            {
                                e.Number += i;
                                e.Number2 += i;
                            }, "Number");
                    });

                var actual = table.Query(partition, row).First();
                Assert.AreEqual(expectedTotal, actual.Number);

                var expectedNumber2 = actual.Untouched;
                Assert.AreEqual(expectedNumber2, actual.Number2);
            }
        }

        [TestMethod]
        public void BatchInsertTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var rnd = new Random();
                var items = 100;

                var toInsert = GetRandomSequence()
                    .SelectMany(r => Enumerable.Range(1, Math.Max(rnd.Next(-2, 10), 1))
                        .Select(i => new { Partition = r, Row = i }))
                    .Take(items)
                    .ToArray();

                // batch insert
                foreach (var inserted in table.BatchInsert(toInsert.Select(r => new { PartitionKey = r.Partition.ToString(), RowKey = r.Row.ToString(), Field = "test" })))
                { }

                var found = toInsert.ToDictionary(i => i.Partition + "|" + i.Row, i => false);
                // check wether the items are in the table
                var entities = table.Query();
                foreach (CyanEntity item in entities)
                    found[item.PartitionKey + "|" + item.RowKey] = true;

                Assert.IsTrue(found.All(f => f.Value));
            }
        }

        [TestMethod]
        public void EmptyTest()
        {
            using (var table = TestUtilities.CreateTestTable())
            {
                var partitions = 10;
                var itemsPerPartition = 100;

                // insert random entities in batches
                var toInsert = GetRandomSequence()
                    .Take(10)
                    .SelectMany(r => Enumerable.Range(0, itemsPerPartition).Select(i => new { Partition = r, Row = i }))
                    .Select(r => new { PartitionKey = r.Partition.ToString(), RowKey = r.Row.ToString(), TestField = "test" });

                foreach (var inserted in table.BatchInsert(toInsert))
                { }

                // check that the items have been inserted
                Assert.AreEqual(partitions * itemsPerPartition, table.Query().Count());

                // empty the table
                table.Empty();

                // check that the table is empty
                Assert.IsTrue(table.Query().Count() == 0);
            }
        }

        static IEnumerable<int> GetRandomSequence()
        {
            Random rnd = new Random();
            while (true)
                yield return rnd.Next(0, 100000);
        }
    }
}
