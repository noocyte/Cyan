using System.Collections.Generic;
using System.Threading.Tasks;
using UXRisk.Lib.Common.Models;

namespace Cyan.Fluent
{
    public interface IFluentCyan
    {
        IFluentCyan IntoTable(string tableName);
        IFluentCyan FromTable(string tableName);
        Task<Response<JsonObject>> PostAsync(JsonObject json);
        Task<Response<JsonObject>> GetByIdAsync(string id);
        Task<Response<IEnumerable<JsonObject>>> GetAllAsync();
        Task<Response<JsonObject>> MergeAsync(JsonObject json);
        Task<Response<JsonObject>> DeleteAsync(JsonObject json);
    }
}