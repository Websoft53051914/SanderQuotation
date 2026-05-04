using Hangfire;
using backend.MESSource;

namespace backend.Common
{
    public class HangfireSchedulerHelper
    {
        /// <summary>
        /// 設定定時工作
        /// </summary>
        /// <param name="scheduleCycleCode"></param>
        /// <param name="cronExpression"></param>
        public void InsertRecurringJob(string scheduleCycleCode, string cronExpression)
        {

            RecurringJob.AddOrUpdate<TransferJob>(
                scheduleCycleCode,
                (job) => job.ExecuteTask(scheduleCycleCode, "Service"),
                cronExpression,
             new RecurringJobOptions
             {
                 TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")
             });
        }

        public void RemoveRecurringJob(string scheduleCycleCode)
        {
            RecurringJob.RemoveIfExists(scheduleCycleCode);
        }
    }
}
