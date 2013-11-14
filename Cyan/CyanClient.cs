using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cyan.Interfaces;
using Cyan.Policies;

namespace Cyan
{
    [DebuggerDisplay("CyanClient({AccountName})")]
    public class CyanClient : ICyanClient
    {
        internal const string DevelopmentStorageAccount = "devstoreaccount1";

        private const string DevelopmentStorageSecret =
            "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        internal CyanRest RestClient;

        /// <summary>
        /// Creates a Cyan client for Azure Table service (the real thing).
        /// </summary>
        /// <param name="accountName">The Azure storage account name.</param>
        /// <param name="accountSecret">The Azure storage account secret.</param>
        /// <param name="useSsl"></param>
        /// <param name="commonRetryPolicy">The retry policy that will be used by default for all operations.</param>
        public CyanClient(string accountName, string accountSecret, bool useSsl = false,
            CyanRetryPolicy commonRetryPolicy = null)
        {
            RestClient = new CyanRest(accountName, accountSecret, useSsl, commonRetryPolicy);
        }

        /// <summary>
        /// Creates a Cyan client for development storage ONLY.
        /// </summary>
        /// <param name="useDevelopmentStorage">Must be set to <code>true</code>.</param>
        public CyanClient(bool useDevelopmentStorage)
        {
            if (!useDevelopmentStorage)
                throw new ArgumentException("Use this constructor for development storage.");

            RestClient = new CyanRest(DevelopmentStorageAccount, DevelopmentStorageSecret);
        }

        /// <summary>
        /// Returns <code>true</code> if the client is using the development storage.
        /// </summary>
        public bool IsDevelopmentStorage
        {
            get { return RestClient.IsDevelopmentStorage; }
        }

        /// <summary>
        /// The name of the account in use.
        /// </summary>
        public string AccountName
        {
            get { return RestClient.AccountName; }
        }

        public string AccountSecret
        {
            get { return RestClient.AccountSecret; }
        }

        /// <summary>
        /// Returns <code>true</code> if the client is using https.
        /// </summary>
        public bool UseSsl
        {
            get { return RestClient.UseSsl; }
        }

        /// <summary>
        /// Creates a reference to a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>Returns a CyanTable for performing operations on entities in thespecified table.</returns>
        /// <remarks>This method does not perform any request.</remarks>
        public ICyanTable this[string tableName]
        {
            get
            {
                CyanUtilities.ValidateTableName(tableName);

                return new CyanTable(tableName, this);
            }
        }

        /// <summary>
        /// Enumerates existing tables.
        /// </summary>
        /// <param name="disableContinuation">If <code>true</code> disables automatic query continuation.</param>
        /// <returns>Returns an enumeration of table names.</returns>
        public async Task<IEnumerable<string>> QueryTables(bool disableContinuation = false)
        {
            bool hasContinuation = false;
            string nextTable = null;
            var tableNames = new List<string>();
            do
            {
                var query = hasContinuation ? string.Format("NextTableName={0}", nextTable) : null;

                var response = await RestClient.GetRequest("Tables", query).ConfigureAwait(false);
                response.ThrowIfFailed();

                hasContinuation = response.Headers.TryGetValue("x-ms-continuation-NextTableName", out nextTable);

                var entities = CyanSerializer.DeserializeEntities(response.ResponseBody.Root);

                tableNames.AddRange(entities.Select(entity => entity.TableName).Cast<string>());

            } while (!disableContinuation && hasContinuation);
            
            return tableNames;
        }

        /// <summary>
        /// Creates a new table.
        /// </summary>
        /// <param name="table">The name of the table to be created.</param>
        public async Task<bool> CreateTable(string table)
        {
            return await CreateTableImpl(table, true).ConfigureAwait(false);
        }



        /// <summary>
        /// Tries to create a new table.
        /// </summary>
        /// <param name="table">The name of the table to be created.</param>
        /// <returns>Returns <code>true</code> if the table was created succesfully,
        /// <code>false</code> if the table already exists.</returns>
        public async Task<bool> TryCreateTable(string table)
        {
            return await CreateTableImpl(table, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an existing table.
        /// </summary>
        /// <param name="table">The name of the table to be deleted.</param>
        public async void DeleteTable(string table)
        {
            CyanUtilities.ValidateTableName(table);
            var resource = string.Format("Tables('{0}')", table);

            var response = await RestClient.DeleteRequest(resource, "").ConfigureAwait(false);
            response.ThrowIfFailed();
        }

        /// <summary>
        /// Creates a CyanClient parsing an Azure Storage connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to be parsed.</param>
        /// <returns>A CyanClient configured via the <code>connectionString</code>.</returns>
        public static ICyanClient FromConnectionString(string connectionString)
        {
            var keyValues = CyanUtilities.ParseConnectionStringKeyValues(connectionString);

            string devStorageValue;
            var devStorage = keyValues.TryGetValue("UseDevelopmentStorage", out devStorageValue)
                             && devStorageValue.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            if (devStorage)
            {
                // development storage
                if (keyValues.ContainsKey("DevelopmentStorageProxyUri"))
                    throw new NotSupportedException("Development storage proxy is not supported.");

                if (keyValues.ContainsKey("AccountName") || keyValues.ContainsKey("AccountKey"))
                    throw new ArgumentException("You cannot specify an account name/key for development storage.");

                return new CyanClient(true);
            }

            // real thing
            var protocol = CyanUtilities.ParseConnectionStringProtocol(keyValues);
            if (protocol == CyanUtilities.SupportedProtocols.NotSupported)
                throw new NotSupportedException("The specified protocol is not supported.");

            var useSsl = protocol == CyanUtilities.SupportedProtocols.Https;

            string accountName;
            if (!keyValues.TryGetValue("AccountName", out accountName))
                throw new ArgumentException("No account name found in connection string.", "connectionString");

            string accountKey;
            if (!keyValues.TryGetValue("AccountKey", out accountKey))
                throw new ArgumentException("No account key found in connection string.", "connectionString");

            if (keyValues.ContainsKey("TableEndpoint"))
                throw new NotSupportedException("Custom table endpoint is not supported by Cyan yet.");

            return new CyanClient(accountName, accountKey, useSsl);
        }

        private async Task<bool> CreateTableImpl(string table, bool throwOnConflict)
        {
            CyanUtilities.ValidateTableName(table);

            var entity = CyanEntity.FromObject(new {TableName = table});

            var document = entity.Serialize();
            var response = await RestClient.PostRequest("Tables", document.ToString()).ConfigureAwait(false);

            if (!throwOnConflict && response.StatusCode == HttpStatusCode.Conflict)
                return false;

            response.ThrowIfFailed();
            return true;
        }
    }
}