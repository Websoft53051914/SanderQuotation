using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace backend.Common.Attribute
{
    public class LogDeletedJobAttribute : JobFilterAttribute, IApplyStateFilter
    {
        // 當任務狀態被「應用/改變」時會觸發此方法
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // 檢查新狀態是不是「已刪除 (Deleted)」
            if (context.NewState is DeletedState deletedState)
            {
                var jobId = context.BackgroundJob.Id;
                var methodName = context.BackgroundJob.Job.Method.Name;

                // 獲取當初被砍的原因（例如：DistributedLockTimeoutException）
                var reason = deletedState.Reason;
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // 狀態移出時觸發，這裡通常不需要寫邏輯
        }
    }
}
