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
        Task<CyanEntity> Merge(CyanEntity entity, bool unconditionalUpdate = false);
        Task Delete(CyanEntity entity);
        string FormatResource(string partitionKey, string rowKey);
    }
}