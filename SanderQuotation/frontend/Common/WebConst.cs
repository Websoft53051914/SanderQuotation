namespace frontend.Common
{
    public class WebConst
    {
        public const string KEY_ALERT = "Alert";

        public const string KEY_ALERTTYPE = "AlertType";

        /// <summary>
        /// bootstrap-datetimepicker.js datetimepicker format
        /// </summary>
        public const string FORMAT_DATETIMEPICKER = "YYYY/MM/DD";

        /// <summary>
        /// bootstrap-datetimepicker.js datetimepicker format
        /// </summary>
        public const string FORMAT_DATETIMEPICKER_TIME = "YYYY/MM/DD HH:mm:ss";

        /// <summary>
        /// 單純顯示的日期格式
        /// </summary>
        public const string FORMAT_DATE = "yyyy/MM/dd";

        /// <summary>
        /// bootstrap-datetimepicker.js datetimepicker format
        /// </summary>
        public const string FORMAT_DATEPICKER = "YYYY-MM";

        #region Api訊息
        public const string SystemErrorMsg = "錯誤，請聯絡系統管理員";
        public const string InsertSuccessMsg = "資料新增成功";
        public const string SaveSuccessMsg = "資料儲存成功";
        public const string DeleteSuccessMsg = "資料刪除成功";
        public const string InvalidSuccessMsg = "資料作廢成功";
        public const string ImportFileSuccessMsg = "檔案上傳成功";
        public const string DeleteFileSuccessMsg = "檔案刪除成功";
        public const string DownLoadFileSuccessMsg = "檔案下載成功";
        public const string SubmitSuccessMsg = "資料送出成功";
        public const string GetDataSuccessMsg = "成功取得查詢資料";
        public const string Success = "執行成功";
        #endregion

#if DEBUG
        public const string FFMPEG_PATH = "ffmpeg/ffmpeg";
#else
        //linux使用
        public const string FFMPEG_PATH = @"/usr/bin/ffmpeg";
#endif

        /// <summary>
        /// 車道電視版型設定預覽資料 localStorage key
        /// </summary>
        public const string KEY_CAR_PREVIEW = "KEY_CAR_PREVIEW";
        /// <summary>
        /// 社區公告版型設定預覽資料 localStorage key
        /// </summary>
        public const string KEY_NOTICE_PREVIEW = "KEY_NOTICE_PREVIEW";
    }
}
