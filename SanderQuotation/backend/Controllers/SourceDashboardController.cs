using AutoMapper;
using Business.BusinessLogic;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace backend.Controllers
{
    [Route("api/eipsource-dashboard")]
    public class SourceDashboardController : BaseProjectController
    {
        private readonly IConfiguration _config;
        public SourceDashboardController(IConfiguration config) : base(config)
        {
            _config = config;

        }
        [HttpGet("GetDashboardData")]
        public IActionResult GetDashboardData()
        {
            try
            {
                var logBL = GetBLInstance<EsScheduleCycleLogBL>();
                var cycleBL = GetBLInstance<EsScheduleCycleBL>();

                var since = DateTime.Now.AddHours(-24);
                var logs = logBL.GetLogsAfter(since);
                var activeSchedules = cycleBL.GetAllActive();

                // --- 統計（以 Detail 為單位，依 JobStatus 分類）---
                var allDetails = logs.SelectMany(l => l.Details).ToList();
                int totalDetails = allDetails.Count;

                int successDetails = allDetails.Count(d => d.JobStatus == "Success");
                int partialFailDetails = allDetails.Count(d => d.JobStatus == "PartialFail");
                int failedDetails = allDetails.Count(d => d.JobStatus == "Failed");

                // Failed 的 Detail 根本沒跑起來，不計入資料錯誤率分母
                var executedDetails = allDetails.Where(d => d.JobStatus != "Failed").ToList();
                int totalDataCount = executedDetails.Sum(d => d.DataCount);
                int totalErrorCount = executedDetails.Sum(d => d.ErrorCount);

                double syncSuccessRate = totalDetails > 0 ? Math.Round((double)successDetails / totalDetails * 100, 1) : 0;
                double dataErrorRate = totalDataCount > 0 ? Math.Round((double)totalErrorCount / totalDataCount * 100, 2) : 0;

                // --- 時間軸資料 (每筆 log + details) ---
                var timelineItems = logs.Select(l => new
                {
                    l.ScheduleCycleCode,
                    RunAt = l.RunAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    l.DurationMs,
                    l.TriggerType,
                    DetailCount = l.Details.Count,
                    TotalData = l.Details.Sum(d => d.DataCount),
                    TotalError = l.Details.Sum(d => d.ErrorCount),
                    HasError = l.Details.Any(d => d.ErrorCount > 0),
                    Details = l.Details.Select(d => new
                    {
                        d.Id,
                        TransferCode = d.GetTransferCode(),
                        TransferType = d.GetTransferTypeDisplay(),
                        d.DataCount,
                        d.ErrorCount,
                        d.DurationMs,
                        d.JobStatus,
                        d.ErrorMessage,
                        RunAt = d.RunAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    })
                }).ToList();

                // --- 按 ScheduleCycleCode 統計成功/失敗 ---
                var jobStats = logs.GroupBy(l => l.ScheduleCycleCode).Select(g => new
                {
                    Code = g.Key,
                    SuccessCount     = g.SelectMany(l => l.Details).Count(d => d.JobStatus == "Success"),
                    PartialFailCount = g.SelectMany(l => l.Details).Count(d => d.JobStatus == "PartialFail"),
                    FailedCount      = g.SelectMany(l => l.Details).Count(d => d.JobStatus == "Failed"),
                    TotalData   = g.SelectMany(l => l.Details).Where(d => d.JobStatus != "Failed").Sum(d => d.DataCount),
                    TotalErrors = g.SelectMany(l => l.Details).Where(d => d.JobStatus != "Failed").Sum(d => d.ErrorCount),
                    TaskBreakdown = g.SelectMany(l => l.Details)
                        .GroupBy(d => new { Code = d.GetTransferCode(), Type = d.GetTransferTypeDisplay() })
                        .Select(t => new
                        {
                            t.Key.Code,
                            t.Key.Type,
                            SuccessCount     = t.Count(d => d.JobStatus == "Success"),
                            PartialFailCount = t.Count(d => d.JobStatus == "PartialFail"),
                            FailedCount      = t.Count(d => d.JobStatus == "Failed"),
                        })
                }).ToList();

                // --- Hangfire recurring jobs 狀態 ---
                var recurringJobs = new List<object>();
                try
                {
                    var taiwanTz = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                    using var connection = JobStorage.Current.GetConnection();
                    var jobs = connection.GetRecurringJobs();
                    recurringJobs = jobs.Select(j => (object)new
                    {
                        j.Id,
                        Cron = j.Cron,
                        LastExecution = j.LastExecution.HasValue
                            ? TimeZoneInfo.ConvertTimeFromUtc(j.LastExecution.Value, taiwanTz).ToString("yyyy-MM-dd HH:mm:ss")
                            : null,
                        NextExecution = j.NextExecution.HasValue
                            ? TimeZoneInfo.ConvertTimeFromUtc(j.NextExecution.Value, taiwanTz).ToString("yyyy-MM-dd HH:mm:ss")
                            : null,
                        LastJobState = j.LastJobState,
                    }).ToList();
                }
                catch { /* Hangfire not available */ }

                return Ok(new
                {
                    Data = new
                    {
                        SyncSuccessRate = syncSuccessRate,
                        DataErrorRate = dataErrorRate,
                        TotalDetails = totalDetails,
                        TotalDataCount = totalDataCount,
                        TotalErrorCount = totalErrorCount,
                        SuccessDetails = successDetails,
                        PartialFailDetails = partialFailDetails,
                        FailedDetails = failedDetails,
                        Timeline = timelineItems,
                        JobStats = jobStats,
                        RecurringJobs = recurringJobs,
                        ActiveSchedules = activeSchedules.Select(s => new
                        {
                            s.ScheduleCycleCode,
                            s.CycleName,
                            s.CronExpression,
                            s.CycleType,
                            LastRunAt = s.LastRunAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                            s.LastRunStatus,
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("GetErrorDetail")]
        public IActionResult GetErrorDetail([FromQuery] Guid detailId)
        {
            try
            {
                var logBL = GetBLInstance<EsScheduleCycleLogBL>();
                var detail = logBL.GetDetailWithErrors(detailId);
                if (detail == null)
                    return NotFound();

                return Ok(new
                {
                    Data = new
                    {
                        TransferCode = detail.GetTransferCode(),
                        TransferType = detail.GetTransferTypeDisplay(),
                        detail.JobStatus,
                        detail.ErrorMessage,
                        detail.DataCount,
                        detail.ErrorCount,
                        ErrorLogs = detail.ErrorLogs.Select(e => new
                        {
                            e.Sql,
                            e.Exception,
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
