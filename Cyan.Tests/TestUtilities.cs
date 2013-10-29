using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyan;
using System.Threading;
using System.IO;
using System.Net;

namespace Cyan.Tests
{
    static class TestUtilities
    {
        static TestUtilities()
        {
            // some performance tricks

            // turn off nagle
            ServicePointManager.UseNagleAlgorithm = false;

            // turn off expect 100 continue
            ServicePointManager.Expect100Continue = false;

            // raise connection limit
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        static bool developmentStorage = false;

        static CyanClient client = null;
        public static CyanClient Client
        {
            get
            {
                if (client == null)
                {
                    if (developmentStorage)
                    {
                        Interlocked.CompareExchange(ref client, new CyanClient(true), null);
                    }
                    else
                    {
                        var lines = File.ReadAllLines(@"..\..\..\AzureAccount.txt");
                        string account = lines[0];
                        string secret = lines[1];

                        Interlocked.CompareExchange(ref client, new CyanClient(account, secret, true), null);
                    }
                }

                return client;
            }
        }

        public static string GetRandomTableName()
        {
            return "CyanTest" + Guid.NewGuid().ToString().Replace("-", "");
        }

        public static DisposableTable CreateTestTable()
        {
            var tableName = GetRandomTableName();
            var ret = new DisposableTable(tableName, Client);

            Client.TryCreateTable(ret.TableName);

            return ret;
        }

        public class DisposableTable : CyanTable, IDisposable
        {
            public DisposableTable(string tableName, CyanClient client)
                : base(tableName, client)
            { }

            #region IDisposable Members

            public void Dispose()
            {
                Client.DeleteTable(TableName);
            }

            #endregion
        }
    }
}
