using backend.Common;
using backend.Common.ConfigurationHelper;
using Business.BusinessLogic;
using Const;
using static Const.Enums;

namespace backend.Jobs
{
    /// <summary>
    /// 資料清理排程工作
    /// 每天凌晨 3 點執行：
    /// 1. 刪除 15 天前的 tb_controllog
    /// 2. 刪除 Status = 9 (作廢) 的 tb_bomfiledecisionlog
    /// 3. 刪除 status &lt;&gt; 1 且 5 天以前的 HistoryFile 及其實體檔案
    /// 4. 刪除 status &lt;&gt; 1 且 5 天以前的 EsFileTransferUpload 及其實體檔案
    /// 5. 刪除 30 天前的 ailog (不論 status)，確保資料庫不會累積過多無用資料
    /// 6. 刪除超過 ExternalQuotation:ExpirationDay 天的 TBBomFileQuotationExternalHistory 紀錄
    /// 7. 刪除超過 DataCleanupSettings:AIFileRetentionDays 天的 AI 產生 Excel 暫存檔案
    /// 8. 刪除超過 15 天的 排程執行 紀錄
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
            ConfigurationHelper configurationHelper = new ConfigurationHelper(_config);
            int ControlLogRetentionDays = _config.GetValue("DataCleanupSettings:ControlLogRetentionDays",15);
            int HistoryFileRetentionDays = _config.GetValue("DataCleanupSettings:HistoryFileRetentionDays", 5);
            int EsFileTransferUploadRetentionDays = _config.GetValue("DataCleanupSettings:EsFileTransferUploadRetentionDays", 5);
            int AILogRetentionDays  = _config.GetValue("DataCleanupSettings:AILogRetentionDays", 30);
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

                try
                {
                    // 刪除 30 天前的 ailog (不論 status)
                    AILogBL aiLogBL = BLFactory.GetInstanceBackGround<AILogBL>(_config);
                    aiLogBL.DeleteOldLog(AILogRetentionDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除超過效期天數的外部查價歷史紀錄
                    int externalExpirationDays = configurationHelper.GetIntValue("ExternalQuotation:ExpirationDay");
                    TBBomFileQuotationExternalHistoryBL externalHistoryBL = BLFactory.GetInstanceBackGround<TBBomFileQuotationExternalHistoryBL>(_config);
                    externalHistoryBL.DeleteOldRecords(externalExpirationDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除 AI 產生的 Excel 暫存檔案（超過設定保留天數）
                    int aiFileRetentionDays = configurationHelper.GetIntValue("DataCleanupSettings:AIFileRetentionDays");
                    string aiExcelDir = _pathProvider.AIExcel;
                    DateTime cutoff = DateTime.Now.AddDays(-aiFileRetentionDays);
                    if (Directory.Exists(aiExcelDir))
                    {
                        foreach (string file in Directory.GetFiles(aiExcelDir))
                        {
                            if (File.GetCreationTime(file) < cutoff)
                                File.Delete(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }

                try
                {
                    // 刪除超過 15 天的 排程執行 紀錄 (依外鍵順序：esTransferErrorLog → esScheduleCycleLogDetail → esScheduleCycleLog)，三個刪除為同一交易
                    int cycleLogDays = 15;
                    EsScheduleCycleLogBL cycleLogBL = BLFactory.GetInstanceBackGround<EsScheduleCycleLogBL>(_config);
                    cycleLogBL.DeleteOldScheduleLogs(cycleLogDays);
                }
                catch (Exception ex)
                {
                    Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                }
            });
        }
    }
}
