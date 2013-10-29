using System.Collections.Generic;

namespace Cyan.Interfaces
{
    public interface ICyanTable
    {
        ICyanRest RestClient { get; }
    
        string TableName { get; }

        /// <summary>
        /// Queries entities in a table.
        /// </summary>
        /// <param name="partition">The partition-key.</param>
        /// <param name="row">The row key.</param>
        /// <param name="filter">The query expression.</param>
        /// <param name="top">Maximum number of entities to be returned.</param>
        /// <param name="disableContinuation">If <code>true</code> disables automatic query continuation.</param>
        /// <param name="fields">Names of the properties to be returned.</param>
        /// <returns>Entities matching your query.</returns>
        IEnumerable<object> Query(string partition = null,
            string row = null,
            string filter = null,
            int top = 0,
            bool disableContinuation = false,
            params string[] fields);

        /// <summary>
        /// Inserts a new entity into a table.
        /// </summary>
        /// <param name="entity">The entity to be inserted.</param>
        /// <returns>The entity that has been inserted.</returns>
        dynamic Insert(object entity);

        bool TryInsert(object entity);
        bool TryInsert(object entity, out dynamic insertedEntity);

        /// <summary>
        /// Updates an existing entity in a table replacing it.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <param name="unconditionalUpdate">If set to <code>true</code> optimistic concurrency is off.</param>
        void Update(object entity, bool unconditionalUpdate = false);

        /// <summary>
        /// Tries to update an existing entity in a table replacing it.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <returns><code>true</code> if the entity ETag matches.</returns>
        bool TryUpdate(object entity);

        /// <summary>
        /// Updates an existing entity in a table by updating the entity's properties.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <param name="unconditionalUpdate">If set to <code>true</code> optimistic concurrency is off.</param>
        /// <param name="fields">The name of the fields to be updated.</param>
        void Merge(object entity, bool unconditionalUpdate = false, params string[] fields);

        /// <summary>
        /// Tries to update an existing entity in a table by updating the entity's properties.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <param name="fields">The name of the fields to be updated.</param>
        /// <returns><code>true</code> if the entity ETag matches.</returns>
        bool TryMerge(object entity, params string[] fields);

        /// <summary>
        /// Deletes an existing entity from a table.
        /// </summary>
        /// <param name="entity">The entity to be deleted.</param>
        /// <param name="unconditionalUpdate">If set to <code>true</code> optimistic concurrency is off.</param>
        void Delete(object entity, bool unconditionalUpdate = false);

        /// <summary>
        /// Deletes an existing entity from a table.
        /// </summary>
        /// <param name="partition">The partition-key of the entity to be deleted.</param>
        /// <param name="row">The row-key of the entity to be deleted.</param>
        /// <param name="eTag">The ETag to be passed as "If-Match" header. Omit or <code>null</code> for "*".</param>
        void Delete(string partition, string row, string eTag = null);

        dynamic InsertOrUpdate(object entity);
        dynamic InsertOrMerge(object entity, params string[] fields);
        ICyanEGT Batch();
        string FormatResource(string partitionKey, string rowKey);
    }
}