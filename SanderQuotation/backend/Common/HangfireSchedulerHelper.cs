using Const;
using Hangfire;
using Hangfire.Storage;
using static Const.Enums;

namespace backend.Common
{
    public class HangfireSchedulerHelper
    {
        /// <summary>
        /// 設定定時工作
        /// </summary>
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

        /// <summary>
        /// 取得多個 ScheduleCycleCode 的即時執行狀態
        /// 結合 RecurringJob 歷史 + BackgroundJob 即時 Processing 狀態
        /// </summary>
        public Dictionary<string, JobStatusInfo> GetJobStatuses(IEnumerable<string> scheduleCycleCodes)
        {
            var result = new Dictionary<string, JobStatusInfo>(StringComparer.OrdinalIgnoreCase);

            // 1. 從 RecurringJob 取得上次執行資訊
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();
            var recurringDict = recurringJobs.ToDictionary(j => j.Id, j => j, StringComparer.OrdinalIgnoreCase);

            // 2. 從 MonitoringApi 取得目前正在 Processing 的 BackgroundJob
            //    (含 BackgroundJob.Enqueue 手動觸發 + RecurringJob 觸發)
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue);

            // 解析 Processing Job 的第一個參數（ScheduleCycleCode）
            var processingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, processingJob) in processingJobs)
            {
                var args = processingJob.Job?.Args;
                if (args != null && args.Count > 0 && args[0] is string code)
                    processingCodes.Add(code);
            }

            // 3. 合併兩個來源，對應至 ScheduleCycleStateEnum（2 分法）
            foreach (var cycleCode in scheduleCycleCodes)
            {
                var info = new JobStatusInfo { ScheduleCycleCode = cycleCode };

                info.State = processingCodes.Contains(cycleCode)
                    ? ScheduleCycleStateEnum.Processing
                    : ScheduleCycleStateEnum.Idle;

                if (recurringDict.TryGetValue(cycleCode, out var job))
                {
                    info.LastRunAt = job.LastExecution;
                    info.NextRunAt = job.NextExecution;
                    info.LastJobId = job.LastJobId;
                    info.LastJobState = job.LastJobState;
                }

                result[cycleCode] = info;
            }

            return result;
        }
    }

    public class JobStatusInfo
    {
        public string ScheduleCycleCode { get; set; }
        /// <summary>對應 ScheduleCycleStateEnum：UnProcessing / Processing / Processed</summary>
        public ScheduleCycleStateEnum State { get; set; }
        /// <summary>Hangfire 原始狀態字串，例如 Succeeded / Failed（供前端細分顯示）</summary>
        public string LastJobState { get; set; }
        public DateTime? LastRunAt { get; set; }
        public DateTime? NextRunAt { get; set; }
        public string LastJobId { get; set; }
    }
}
