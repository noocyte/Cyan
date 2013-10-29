namespace Cyan.Interfaces
{
    public interface ICyanEGT
    {
        dynamic Insert(object entity);
        void InsertOrUpdate(object entity);
        void Update(object entity, bool unconditionalUpdate = false);
        void Merge(object entity, bool unconditionalUpdate = false);
        void Delete(object entity, bool unconditionalUpdate = false);
        void Delete(string partitionKey, string rowKey, string eTag = null);
        void Commit();
        bool TryCommit();
    }
}