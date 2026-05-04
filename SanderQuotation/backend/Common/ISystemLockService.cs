namespace backend.Common
{
    public interface ISystemLockService
    {
        bool IsLocked(string userId);
        void Lock(string userId);
        void Unlock(string userId);
    }

}
