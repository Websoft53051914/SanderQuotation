using System.ComponentModel;

namespace Const
{
    public class Enums
    {
        /// 0:查询,1:新增,2:修改,3:删除
        public enum LogAction
        {
            [Description("查询")]
            Query = 0,

            [Description("新增")]
            Create = 1,

            [Description("修改")]
            Edit = 2,

            [Description("删除")]
            Delete = 3
        }


        public enum AccountStatusEnum
        {
            [Description("啟用")]
            Enabled = 1,

            [Description("停用")]
            Disabled = 8,

            [Description("作廢")]
            Cancel = 9
        }

        /// <summary>
        /// 值=table內id
        /// </summary>
        public enum FuncID
        {
            Camera_View = 0,

            Home_View = 0,
            ResetTESTP = 1,
            Mechanic_View = 2,

            CentralMonitoring_View = 5,

            MessagePool_View = 3,

            Permission_View = 990001,
            Permission_Create = 990002,
            Permission_Edit = 990003,
            Permission_Delete = 990004,

            //系統功能維護
            SysFunc_View = 990011,
            SysFunc_Create = 990012,
            SysFunc_Edit = 990013,
            SysFunc_Delete = 990014,

            //系統角色設定
            SysFuncRole_View = 990021,
            SysFuncRole_Create = 990022,
            SysFuncRole_Edit = 990023,
            SysFuncRole_Delete = 990024,

            //Log查詢
            Log_View = 990031,

            //UI範例
            UIExample_View = 990041,
            UIExample_Create = 990042,
            UIExample_Edit = 990043,
            UIExample_Delete = 990044,

            //系統功能維護
            SysFuncClass_View = 990051,
            SysFuncClass_Create = 990052,
            SysFuncClass_Edit = 990053,
            SysFuncClass_Delete = 990054,

            //LOG
            ControlLog_View = 990061,
            ControlLog_Create = 990062,
            ControlLog_Edit = 990063,
            ControlLog_Delete = 990064,

            //資料庫設定
            ESDbTransfer_View = 990071,
            ESDbTransfer_Create = 990072,
            ESDbTransfer_Edit = 990073,
            ESDbTransfer_Delete = 990074,

            //資料庫轉檔設定
            ESDbTransferMapping_View = 990081,
            ESDbTransferMapping_Create = 990082,
            ESDbTransferMapping_Edit = 990083,
            ESDbTransferMapping_Delete = 990084,

            //排程設定
            Cyclesettings_View =   990091,
            Cyclesettings_Create = 990092,
            Cyclesettings_Edit =   990093,
            Cyclesettings_Delete = 990094,

            //DashBoard設計
            DashBoard_View =   990121,
            DashBoard_Create = 990122,
            DashBoard_Edit =   990123,
            DashBoard_Delete = 990124,

            //DashBoard監看
            DashboardHome_View =   990131,
            DashboardHome_Create = 990132,
            DashboardHome_Edit =   990133,
            DashboardHome_Delete = 990134,

            FoupLot_View =   990181,
            FoupLot_Create = 990182,
            FoupLot_Edit =   990183,
            FoupLot_Delete = 990184,

            ProcessSettingManagement_View =   990191,
            ProcessSettingManagement_Create = 990192,
            ProcessSettingManagement_Edit =   990193,
            ProcessSettingManagement_Delete = 990194,

            //檔案轉入資料表對應設定
            TableExcel_View = 990201,
            TableExcel_Create = 990202,
            TableExcel_Edit = 990203,
            TableExcel_Delete = 990204,

            HistoryFile_View = 990211,
            HistoryFile_Create = 990212,
            HistoryFile_Edit = 990213,
            HistoryFile_Delete = 990214,

            //開發人員依此使用值，避免一個value被兩個功能使用，導致開啟一個權限卻可以使用兩個功能
            // Hugh 20000 LEO 30000 Jerri 40000 Jordan 50000 Ray 60000

            // 轉入檔案上傳
            EsFileTransferUpload_View = 40001,
            EsFileTransferUpload_Create = 40002,
            EsFileTransferUpload_Edit = 40003,
            EsFileTransferUpload_Delete = 40004,
        }

        /// <summary>
        /// 狀態項目
        /// </summary>
        public enum StatusEnum
        {
            [Description("停用")]
            /// <summary>
            /// 停用
            /// </summary>
            Disabled = 0,

            [Description("啟用")]
            /// <summary>
            /// 啟用
            /// </summary>
            Enabled = 1,


            [Description("作廢")]
            /// <summary>
            /// 作廢
            /// </summary>
            Cancel = 9,
        }

        /// <summary>
        /// 狀態項目
        /// </summary>
        public enum StatusEnumNY
        {
            [Description("Disable")]
            /// <summary>
            /// 停用
            /// </summary>
            Disabled = 0,

            [Description("Enable")]
            /// <summary>
            /// 啟用
            /// </summary>
            Enabled = 1,
        }



        public enum LogStatusEnum
        {
            /// <summary>
            /// 失敗
            /// </summary>
            [Description("失敗")]
            Failed = 0,

            /// <summary>
            /// 成功
            /// </summary>
            [Description("成功")]
            Success = 1,
        }



        /// <summary>
        /// 星期選單
        /// </summary>
        public enum WeekEnum
        {
            [Description("週日")]
            Sunday = 0,

            [Description("周一")]
            Monday = 1,

            [Description("週二")]
            Tuesday = 2,

            [Description("週三")]
            Wednesday = 3,

            [Description("週四")]
            Thursday = 4,

            [Description("週五")]
            Friday = 5,

            [Description("週六")]
            Saturday = 6
        }




        /// <summary>
        /// 性別Enum
        /// </summary>
        public enum GenderEnum
        {
            [Description("男")]
            Male = 1,

            [Description("女")]
            Female = 2
        }

        public enum DBType
        {
            MSSQL = 1,
            MySQL = 2,
        }

        /// <summary>
        /// 儲存動作
        /// </summary>
        public enum SaveActionEnum
        {
            None = 0,
            Create = 1,
            Update = 2,
            Delete = 3,
        }

        public enum NasRequestType
        {
            /// <summary>
            /// 個人
            /// </summary>
            AccoountFolder = 1,

            /// <summary>
            /// 專案
            /// </summary>
            ProjectFolder = 2,

            /// <summary>
            /// 部門
            /// </summary>
            DepartmentFolder = 3,


            /// <summary>
            /// 公司
            /// </summary>
            CompanyFolder = 4,
        }


        /// <summary>
        /// 資源主檔 狀態
        /// </summary>
        public enum ResObjectStatusEnum
        {
            [Description("Available")]
            Available = 1,

            [Description("Disable")]
            Disabled = 0,

            [Description("Maintenance")]
            Maintenance = 2,

            [Description("Scrap")]
            Scrap = 3,
        }

        /// <summary>
        /// 申請類別
        /// </summary>
        public enum CategoryType
        {
            [Description("ResourceCategory")]
            Resource = 1,
            [Description("AnnouncementCategory")]
            Announcement = 2,
        }

        /// <summary>
        /// 簽核動作
        /// </summary>
        public enum SignResult
        {
            /// <summary>
            /// 同意
            /// </summary>
            [Description("Approve")]
            Approve = 1,
            /// <summary>
            /// 駁回
            /// </summary>
            [Description("Rejected")]
            Reject = 2,
            /// <summary>
            /// 退回上一關
            /// </summary>
            [Description("Return_To_Previous_Step")]
            Return = 3
        }

        /// <summary>
        /// 申請單狀態
        /// </summary>
        public enum ResApplyStatus
        {
            /// <summary>
            /// 草稿
            /// </summary>
            Draft = 1,
            /// <summary>
            /// 簽核中
            /// </summary>
            Signing = 2,
            /// <summary>
            /// 核准
            /// </summary>
            Approved = 3,
            /// <summary>
            /// 駁回
            /// </summary>
            Rejected = 4,
            /// <summary>
            /// 已取用
            /// </summary>
            Picked = 5,
            /// <summary>
            /// 歸還
            /// </summary>
            Returned = 6
        }

        /// <summary>
        /// 公告申請單狀態
        /// </summary>
        public enum AnnApplyStatus
        {
            /// <summary>
            /// 草稿
            /// </summary>
            Draft = 0,
            /// <summary>
            /// 簽核中
            /// </summary>
            Signing = 1,
            /// <summary>
            /// 核准
            /// </summary>
            Approved = 2,
            /// <summary>
            /// 駁回
            /// </summary>
            Rejected = 3,

        }

        /// <summary>
        /// 簽核者是否為代理人
        /// </summary>
        public enum DelegateStatusEnum
        {
            /// <summary>
            /// 簽核者為本人
            /// </summary>
            [Description("OriginalUser")]
            NotDelegate = 0,

            /// <summary>
            /// 簽核人為delegateID
            /// </summary>
            [Description("Delegater")]
            Delegate = 1,
        }

        /// <summary>
        /// 轉入檔案上傳 處理狀態
        /// </summary>
        public enum EsFileTransferUploadProcessStatusEnum
        {
            /// <summary>
            /// 未轉檔：檔案已上傳，尚未執行欄位對應轉換
            /// </summary>
            [Description("未轉檔")]
            Pending = 1,

            /// <summary>
            /// 已轉檔：欄位對應完成，並已儲存至資料庫
            /// </summary>
            [Description("已轉檔")]
            Transferred = 2,

            /// <summary>
            /// 未查料：已轉檔，尚未執行查詢採購型號作業（只有 BOM 檔案有此狀態）
            /// </summary>
            [Description("未查料")]
            PendingPartSearch = 3,

            /// <summary>
            /// 已查料：查詢採購型號作業完成（只有 BOM 檔案有此狀態）
            /// </summary>
            [Description("已查料")]
            PartSearchDone = 4,

            /// <summary>
            /// 已查價：內部 + 外部查價完成
            /// </summary>
            [Description("已查價")]
            PricingDone = 5,
        }

        /// <summary>
        /// 轉檔檔案類型
        /// </summary>
        public enum TransferFileTypeEnum
        {
            /// <summary>
            /// 未知
            /// </summary>
            [Description("Unknown")]
            Unknown = 0,

            /// <summary>
            /// Excel 2007+ (.xlsx)
            /// </summary>
            [Description(".xlsx")]
            Xlsx = 1,

            /// <summary>
            /// Excel 97-2003 (.xls)
            /// </summary>
            [Description(".xls")]
            Xls = 2,

            /// <summary>
            /// CSV (.csv)
            /// </summary>
            [Description(".csv")]
            Csv = 3,
        }


        public enum EsDbTransferDbTypeEnum
        {
            [Description("PostgreSQL")]
            PostgreSQL = 1 ,
            [Description("MSSQL")]
            MSSQL = 2
        }

    }
}
