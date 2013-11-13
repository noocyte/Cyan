using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cyan.Interfaces
{
    public interface ICyanTable
    {
        ICyanRest RestClient { get; }
        string TableName { get; }
        Task<IEnumerable<CyanEntity>> Query(string partition = null,
            string row = null,
            string filter = null,
            int top = 0,
            bool disableContinuation = false,
            params string[] fields);
        Task<CyanEntity> Insert(CyanEntity entity);

        //Task<bool> TryInsert(CyanEntity entity);
        //Task<bool> TryInsert(CyanEntity entity, out CyanEntity insertedEntity);
        //void Update(CyanEntity entity, bool unconditionalUpdate = false);
        //Task<CyanEntity> TryUpdate(CyanEntity entity);
        //void Merge(CyanEntity entity, bool unconditionalUpdate = false, params string[] fields);
        //Task<CyanEntity> TryMerge(CyanEntity entity, params string[] fields);
        void Delete(CyanEntity entity, bool unconditionalUpdate = false);
        void Delete(string partition, string row, string eTag = null);
        Task<CyanEntity> InsertOrUpdate(CyanEntity entity);
        Task<CyanEntity> InsertOrMerge(CyanEntity entity, params string[] fields);
        ICyanEGT Batch();
        string FormatResource(string partitionKey, string rowKey);
    }
}