using backend.Common;
using Business.BusinessLogic;
using Const;
using DocumentFormat.OpenXml.Presentation;
using static Const.Enums;

namespace backend.EIPSource
{
    /// <summary>
    /// 資料清理排程工作
    /// 每天凌晨 3 點執行：
    /// 1. 刪除 15 天前的 tb_controllog
    /// 2. 刪除 Status = 9 (作廢) 的 tb_bomfiledecisionlog
    /// 3. 刪除 15 天前的 esScheduleCycleLogDetail
    /// 4. 刪除 15 天前的 esScheduleCycleLog
    /// 5. 刪除 status &lt;&gt; 1 且 5 天以前的 HistoryFile 及其實體檔案
    /// 6. 刪除 status &lt;&gt; 1 且 5 天以前的 EsFileTransferUpload 及其實體檔案
    /// </summary>
    public class DataCleanupJob
    {
        /// <summary>
        /// Log 中的 Controller 名稱，方便識別是哪個工作產生的 Log
        /// </summary>
        private const string LogControllerName = nameof(DataCleanupJob);

        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly PathProvider _pathProvider;

        public DataCleanupJob(IConfiguration config, IWebHostEnvironment env, PathProvider pathProvider)
        {
            _config = config;
            _env = env;
            _pathProvider = pathProvider;
        }

        public async Task ExecuteAsync()
        {   
            int ControlLogRetentionDays = _config.GetValue<int>("DataCleanupSettings:ControlLogRetentionDays",15);
            int CycleLogRetentionDays = _config.GetValue<int>("DataCleanupSettings:CycleLogRetentionDays",15);
            int HistoryFileRetentionDays = _config.GetValue<int>("DataCleanupSettings:HistoryFileRetentionDays", 5);
            int EsFileTransferUploadRetentionDays = _config.GetValue<int>("DataCleanupSettings:EsFileTransferUploadRetentionDays", 5);
            await Task.Run(() =>
            {
                try
                {
                    // 刪除 15 天前的 tb_controllog
                    LogBL logBL = BLFactory.GetInstanceBackGround<LogBL>(_config);
                    logBL.DeleteOldLog(ControlLogRetentionDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除 Status = 9 (作廢) 的 tb_bomfiledecisionlog
                    TBBomFileDecisionLogBL decisionLogBL = BLFactory.GetInstanceBackGround<TBBomFileDecisionLogBL>(_config);
                    decisionLogBL.PhysicalDeleteByStatus((int)StatusEnum.Cancel);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 先刪 detail 再刪 master，避免 FK 問題
                    EsScheduleCycleLogBL cycleLogBL = BLFactory.GetInstanceBackGround<EsScheduleCycleLogBL>(_config);
                    cycleLogBL.DeleteOldLogDetail(CycleLogRetentionDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    EsScheduleCycleLogBL cycleLogBL = BLFactory.GetInstanceBackGround<EsScheduleCycleLogBL>(_config);
                    cycleLogBL.DeleteOldLog(CycleLogRetentionDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除 status <> 1 且 5 天以前的 HistoryFile 記錄及實體檔案
                    string historyFileDir = Path.Combine(_env.ContentRootPath, FileDirectoryConst.HistoryFile);
                    HistoryFileBL historyFileBL = BLFactory.GetInstanceBackGround<HistoryFileBL>(_config);
                    historyFileBL.DeleteOldNonActiveFiles(HistoryFileRetentionDays, historyFileDir);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除 status <> 1 且 5 天以前的 EsFileTransferUpload 記錄及實體檔案
                    EsFileTransferUploadBL esFileTransferUploadBL = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>(_config);
                    esFileTransferUploadBL.DeleteOldNonActiveFiles(EsFileTransferUploadRetentionDays, _pathProvider.EsFileTransferUpload);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }
            });
        }
    }
}
