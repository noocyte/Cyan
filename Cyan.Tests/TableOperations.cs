using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyan.Tests
{
    [TestClass]
    public class TableOperations
    {
        [TestMethod]
        public void CreateTableTest()
        {
            var tableId = TestUtilities.GetRandomTableName();

            // create a table
            Assert.IsTrue(TestUtilities.Client.TryCreateTable(tableId));

            // check that the table exists
            Assert.IsTrue(TestUtilities.Client.QueryTables().Any(t => t == tableId));

            // delete the table (cleanup)
            TestUtilities.Client.DeleteTable(tableId);
        }

        [TestMethod]
        public void DeleteTableTest()
        {
            var tableId = TestUtilities.GetRandomTableName();

            // create a table
            Assert.IsTrue(TestUtilities.Client.TryCreateTable(tableId));

            // delete the table
            TestUtilities.Client.DeleteTable(tableId);

            // check that the table does not exist anymore
            Assert.IsFalse(TestUtilities.Client.QueryTables().Any(t => t == tableId));
        }

        [TestMethod]
        public void DuplicateTableTest()
        {
            var tableId = TestUtilities.GetRandomTableName();

            // create a new table
            Assert.IsTrue(TestUtilities.Client.TryCreateTable(tableId));

            // try to create a table with the same id
            Assert.IsFalse(TestUtilities.Client.TryCreateTable(tableId));

            // check that the table exists
            Assert.IsTrue(TestUtilities.Client.QueryTables().Any(t => t == tableId));

            // delete the table
            TestUtilities.Client.DeleteTable(tableId);

            // check that the table does not exist anymore
            Assert.IsFalse(TestUtilities.Client.QueryTables().Any(t => t == tableId));
        }

        [TestMethod]
        public void QueryTablesTest()
        {
            var tableIds = Enumerable.Range(0, 5)
                .Select(t => TestUtilities.GetRandomTableName())
                .ToArray();

            // create some random tables
            foreach (var id in tableIds)
                Assert.IsTrue(TestUtilities.Client.TryCreateTable(id));

            // query existing tables
            var tables = TestUtilities.Client.QueryTables().ToArray();

            // check that all created tables exist
            Assert.IsTrue(tableIds.All(t => tables.Contains(t)));

            // delete all created tables
            foreach (var id in tableIds)
                TestUtilities.Client.DeleteTable(id);

            // check that deleted tables does not exist anymore
            var afterdel = TestUtilities.Client.QueryTables().ToArray();
            Assert.IsTrue(tableIds.All(t => !afterdel.Contains(t)));
        }
    }
}
