using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cyan.Interfaces
{
    public interface ICyanClient
    {
        /// <summary>
        /// Returns <code>true</code> if the client is using the development storage.
        /// </summary>
        bool IsDevelopmentStorage { get; }

        /// <summary>
        /// The name of the account in use.
        /// </summary>
        string AccountName { get; }
        string AccountSecret { get; }
        

        /// <summary>
        /// Returns <code>true</code> if the client is using https.
        /// </summary>
        bool UseSsl { get; }

        /// <summary>
        /// Creates a reference to a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>Returns an ICyanTable for performing operations on entities in thespecified table.</returns>
        /// <remarks>This method does not perform any request.</remarks>
        ICyanTable this[string tableName] { get; }

        /// <summary>
        /// Enumerates existing tables.
        /// </summary>
        /// <param name="disableContinuation">If <code>true</code> disables automatic query continuation.</param>
        /// <returns>Returns an enumeration of table names.</returns>
        Task<IEnumerable<string>> QueryTables(bool disableContinuation = false);

        /// <summary>
        /// Creates a new table.
        /// </summary>
        /// <param name="table">The name of the table to be created.</param>
        Task<bool> CreateTable(string table);

        /// <summary>
        /// <summary>
        /// Tries to create a new table.
        /// </summary>
        /// <param name="table">The name of the table to be created.</param>
        /// <returns>Returns <code>true</code> if the table was created succesfully,
        /// <code>false</code> if the table already exists.</returns>
        Task<bool> TryCreateTable(string table);

        /// <summary>
        /// Deletes an existing table.
        /// </summary>
        /// <param name="table">The name of the table to be deleted.</param>
        void DeleteTable(string table);
    }
}