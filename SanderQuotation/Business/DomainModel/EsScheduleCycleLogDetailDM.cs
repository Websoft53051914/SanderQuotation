using Const;
using Core.Utility.Utility;
using System.ComponentModel;
using System.Reflection;
using static Const.Enums;

namespace Business.DomainModel
{
    public class EsScheduleCycleLogDetailDM : BaseDM
    {
        public Guid ScheduleCycleLogId { get; set; }

        public int DataCount { get; set; }

        public int ErrorCount { get; set; }

        public string DBTransferCode { get; set; }

        public string FileTransferCode { get; set; }

        /// <summary>其他排程動作類型（對應 ScheduleCycleActionTypeEnum）</summary>
        public int? OtherActionType { get; set; }

        public DateTime RunAt { get; set; }

        public int? DurationMs { get; set; }

        /// <summary>Success / PartialFail / Failed</summary>
        public string JobStatus { get; set; }

        /// <summary>任務層級例外訊息（DB連線失敗等）</summary>
        public string ErrorMessage { get; set; }

        public List<EsTransferErrorLogDM> ErrorLogs = new();

        /// <summary>取得該筆 Detail 的 TransferCode（依優先順序：DB轉入 > 檔案轉入 > 其他動作 Description）</summary>
        public string GetTransferCode()
        {
            if (!string.IsNullOrEmpty(DBTransferCode)) return DBTransferCode;
            if (!string.IsNullOrEmpty(FileTransferCode)) return FileTransferCode;
            if (OtherActionType.HasValue)
                return GetActionTypeDescription(OtherActionType.Value);
            return string.Empty;
        }

        /// <summary>取得中文類型名稱：資料轉入 / 檔案轉入 / 其他動作 Description</summary>
        public string GetTransferTypeDisplay()
        {
            if (!string.IsNullOrEmpty(DBTransferCode)) return "資料轉入";
            if (!string.IsNullOrEmpty(FileTransferCode)) return "檔案轉入";
            if (OtherActionType.HasValue)
                return "其他任務";
            return "未知類型";
        }

        private static string GetActionTypeDescription(int value)
        {
            return EnumUtility.GetDescriptionByInt<ScheduleCycleActionTypeEnum>(value);
        }
    }
}
