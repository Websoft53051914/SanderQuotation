namespace Const
{
    public class DbConst
    {
        /// <summary>
        /// 已刪除/ disabled
        /// </summary>
        public const string DELETED = "Y";

        /// <summary>
        /// 未刪除/ enabled
        /// </summary>
        public const string ALIVE = "N";

        /// <summary>
        /// 其他選項值
        /// </summary>
        public const string OPTION_OTHER = "999";

        /// <summary>
        /// 資料庫時間格式
        /// </summary>
        public const string FORMAT_DATETIME = "yyyyMMdd HHmmss";

        /// <summary>
        /// 日期時間格式
        /// </summary>
        public const string FORMAT_DATETIME2 = "yyyy/MM/dd HH:mm:ss";

        /// <summary>
        /// 時間格式(預定入國日+航班抵達時間用)
        /// </summary>
        public const string FORMAT_SHORTDATETIME = "yyyyMMddHHmm";

        /// <summary>
        /// 資料庫日期格式
        /// </summary>
        public const string FORMAT_DATE = "yyyyMMdd";
        /// <summary>
        /// 顯示日期格式
        /// </summary>
        public const string FORMAT_DATE2 = "yyyy/MM/dd";

        /// <summary>
        /// 資料庫時段 TimeSpan 轉換格式
        /// </summary>
        public const string FORMAT_TIME = "hhmm";

        /// <summary>
        /// 資料庫時段 TimeSpan 轉換格式
        /// </summary>
        public const string FORMAT_TIME_2 = "hhmmss";

        /// <summary>
        /// 資料庫時段 TimeSpan 轉換格式
        /// </summary>
        public const string FORMAT_TIME3 = "HH:mm";

        /// <summary>
        /// 介接資料時間格式
        /// </summary>
        public const string FORMAT_ADAPTER_DATETIME = "yyyyMMddHHmmss";

        /// <summary>
        /// 最大(<)門禁到期時間
        /// </summary>
        public static readonly DateTime MAX_ACCESS_CONTROL_END_TIME = new(2038, 1, 1);

        /// <summary>
        /// 最大卡號(比對卡指令格式最大 FFFFFFFF(16))
        /// </summary>
        public const long MAX_CARD_NO = 4294967295;

        /// <summary>
        /// 最大繼電器閉合時間(比對卡指令格式最大 FFFF(16))
        /// </summary>
        public const int MAX_RELAY_CLOSING_TIME = 65535;

        /// <summary>
        /// 最大下掛模組數量(比對卡指令格式最大 FF(16))
        /// </summary>
        public const int MAX_MODULE_NUM = 255;

        /// <summary>
        /// 辨識機最大檔案大小
        /// </summary>
        public const string MAX_FILE_SIZE_DEVICE_ACCESS = "200KB";

        /// <summary>
        /// 上傳檔案最大檔案大小
        /// </summary>
        public const string MAX_FILE_SIZE_UPLOAD_FILE = "300MB";

        /// <summary>
        /// 設定加密
        /// </summary>
        public const string SALT_CONFIG = "d2ff6607f86d4d4cb5a592ed97ed89d3";

        /// <summary>
        /// 狀態項目顯示文字參照
        /// </summary>
        public static Dictionary<string, string> GetRefYNDisplayText()
        {
            return new()
            {
                { "Y", "是" },
                { "N", "否" },
            };
        }

        /// <summary>
        /// LED 歡迎詞替換社區名稱關鍵字
        /// </summary>
        public static string LED_COMMUNITY_KEYWORD = "[社區名稱]";


        public static string MYSQL_MIN_DATE = "1000-01-01";
        public static string MYSQL_MAX_DATE = "9999-12-31";


        public static List<string> TableExecuteSQL = new List<string>() {
                @"CREATE TABLE IF NOT EXISTS `tb_car` (
  `Id` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `ETCId` varchar(100) DEFAULT NULL COMMENT 'ETC ID',
  `ETCTagId` varchar(100) DEFAULT NULL COMMENT 'ETC 標籤Id',
  `LicenseNo` varchar(100) NOT NULL COMMENT '車牌號碼',
  `ValidStartDate` date NOT NULL COMMENT '開始日期',
  `ValidEndDate` date NOT NULL COMMENT '結束日期',
  `CommunityRoomId` varchar(36) DEFAULT NULL COMMENT '門牌Id (TB_CommunityRoom.Id)',
  `Status` int NOT NULL COMMENT '狀態',
  `Remark` varchar(1000) DEFAULT NULL COMMENT '備註',
  PRIMARY KEY (`Id`,`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='車輛資料'; ",
@"CREATE TABLE IF NOT EXISTS `tb_cardevice_m` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Name` varchar(100) NOT NULL COMMENT '位置',
  `Type` varchar(100) NOT NULL COMMENT '設備類型',
  `IP` varchar(100) DEFAULT NULL COMMENT 'IP',
  `Port` varchar(100) DEFAULT NULL COMMENT 'PORT',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Remark` varchar(100) DEFAULT NULL COMMENT '備註',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `DeviceSystemId` varchar(36) DEFAULT NULL COMMENT 'DeviceSystem 綁定時回寫',
  `ConnectStatus` int NOT NULL DEFAULT '0' COMMENT '連線狀態 0.離線 1.在線',
  `ConnectErrorTimes` int NOT NULL DEFAULT '0' COMMENT '偵測失敗次數',
  `ConnectException` varchar(5000) DEFAULT NULL,
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '車道設備主檔'; ",
@"CREATE TABLE IF NOT EXISTS `tb_cardevicesystem` (
  `Id` varchar(36) NOT NULL,
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `DeviceSystemName` varchar(100) NOT NULL COMMENT '設備系統名稱',
  `DeviceSystemType` int NOT NULL COMMENT '設備系統類別',
  `CommunityBuildingId` varchar(36) DEFAULT NULL COMMENT '社區建物資料代號(CommunityBuilding.Id)',
  `Remark` varchar(1000) DEFAULT NULL,
  `Status` int NOT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL,
  `Updater` varchar(36) NOT NULL,
  `UpdateTime` datetime NOT NULL,
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '設備系統主表資料'; ",
@"CREATE TABLE IF NOT EXISTS `tb_cardevicesystemlane` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `DeviceCarETCRecognitionId` varchar(36) DEFAULT NULL COMMENT 'ETC',
  `DeviceCarLicenseRecognitionId` varchar(36) DEFAULT NULL COMMENT '車牌辨識機',
  `DeviceCarLEDCCDeviceId` varchar(36) DEFAULT NULL COMMENT 'LED字幕機',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Status` int NOT NULL COMMENT '狀態',
  `Name` varchar(100) NOT NULL,
  `CarDeviceSystemId` varchar(36) NOT NULL,
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `DeviceCarBarrierGateId` varchar(36) DEFAULT NULL COMMENT '車道閘門',
  `DeviceCarIPCameraId` varchar(36) DEFAULT NULL COMMENT 'IP Camera',
  `CarTVDisplaySettingId` varchar(36) DEFAULT NULL COMMENT '車道電視版型',
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '車道系統'; ",
@"CREATE TABLE IF NOT EXISTS `tb_carinoutlog` (
  `Id` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL,
  `Creator` varchar(36) NOT NULL,
  `UpdateTime` datetime NOT NULL,
  `Updater` varchar(36) NOT NULL,
  `LicenseNo` varchar(100) DEFAULT NULL COMMENT '車牌號碼',
  `ETCId` varchar(100) DEFAULT NULL COMMENT 'ETC ID',
  `Direction` int DEFAULT NULL COMMENT '進出方向(1:順向,2:反向)',
  `ActionTime` datetime NOT NULL COMMENT '進出時間',
  `ListType` int DEFAULT NULL COMMENT '名單來源(1:白名單,2:黑名單)',
  `CarDevice_M_Id` varchar(36) NOT NULL COMMENT '車道設備Id(FK TB_CarDevice_M.Id)',
  `ETCTagId` varchar(100) DEFAULT NULL COMMENT 'ETC 標籤Id',
  `CarDeviceSystemLaneId` varchar(36) DEFAULT NULL COMMENT '車道連動資料(FK TB_CarDeviceSystemLane.Id)',
  `CarDeviceSystemLaneName` varchar(100) DEFAULT NULL COMMENT '車道連動資料名稱',
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '車輛進出紀錄'; ",
@"CREATE TABLE IF NOT EXISTS `tb_carinoutlogimage` (
  `Id` varchar(36) NOT NULL,
  `CarInOutLogId` varchar(36) NOT NULL COMMENT '車輛進出紀錄Id',
  `File` mediumblob COMMENT '檔案內容',
  `FileExt` varchar(100) NOT NULL COMMENT '檔案副檔名',
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '車輛進出紀錄圖片'; ",
@"CREATE TABLE IF NOT EXISTS `tb_carsystemlane` (
  `Id` varchar(36) NOT NULL,
  `CarId` varchar(36) NOT NULL COMMENT '車輛Id(FK TB_Car.Id)',
  `CarDeviceSystemLaneId` varchar(36) NOT NULL COMMENT '車道連動資料(FK TB_CarDeviceSystemLane.Id)',
  PRIMARY KEY(`Id`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COMMENT = '車輛可進出車道資料'; ",
@"CREATE TABLE IF NOT EXISTS `tb_cartvdisplaysetting` (
  `Id` varchar(36) NOT NULL COMMENT '唯一識別碼',
  `CarTvDisplaySettingName` varchar(100) DEFAULT NULL COMMENT '版型名稱',
  `CategoryCsv` text COMMENT '分類',
  `ShowHeaderPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示標題區塊',
  `SubHeaderType` int NOT NULL COMMENT '副標題顯示類型',
  `MainHeaderTextHtml` text COMMENT '主標題文字樣式',
  `SubHeaderTextHtml` text COMMENT '副標題文字樣式',
  `ShowImagePanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示車道影像區塊區塊',
  `ShowSignPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示燈號區塊',
  `SignType` int NOT NULL COMMENT '燈號區塊顯示類型',
  `SignSecondTextHtml` text COMMENT '燈號區塊倒數秒數文字樣式',
  `ShowSignTextPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示燈號文字訊息區塊',
  `SignTextTextHtml` text COMMENT '燈號文字訊息文字樣式',
  `ShowInfoPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示溫度濕度狀態區塊',
  `InfoTextHtml` text COMMENT '溫度濕度狀態文字樣式',
  `ShowAlertPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示通知與警告區塊',
  `AlertTextHtml` text COMMENT '通知與警告區塊文字樣式',
  `EmergencyDataSql` text COMMENT '緊急事件資料 SQL',
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `Status` int NOT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL COMMENT '建立人員',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新人員',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `InfoValueTextHtml` text COMMENT '量測值文字樣式',
  `DisplayMeasurementValueCsv` varchar(200) DEFAULT NULL COMMENT '顯示的量測值',
  `MainHeaderText` varchar(50) DEFAULT NULL COMMENT '主標題文字',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='車道電視版型設定';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecarbarriergate` (
  `Id` varchar(36) NOT NULL,
  `Device_M_Id` varchar(36) NOT NULL,
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL,
  `Updater` varchar(36) NOT NULL,
  `UpdateTime` datetime NOT NULL,
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `CarOutReciprocalSeconds` int NOT NULL COMMENT '出車倒數秒數',
  `CarInReciprocalSeconds` int NOT NULL COMMENT '入車倒數秒數',
  `SlaveId` int DEFAULT '0',
  `Type` int NOT NULL DEFAULT '1' COMMENT '類別(1:車道系統,2:車道預警系統)',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='車道閘門';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecaretcrecognition` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL COMMENT 'tb_device_m.Id',
  `CommunityUid` varchar(36) DEFAULT NULL COMMENT '社區識別代號',
  `IsCardOpen` tinyint NOT NULL DEFAULT '0' COMMENT '是否見卡開',
  `RelayClosingTime` int NOT NULL,
  `HeartBeatInterval` int NOT NULL DEFAULT '0' COMMENT '設備心跳包間隔秒數',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='ETC辨識';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecaripcamera` (
  `Id` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL,
  `UpdateTime` datetime NOT NULL,
  `Creator` varchar(36) NOT NULL,
  `Updater` varchar(36) NOT NULL,
  `ACC` varchar(100) NOT NULL COMMENT '帳號',
  `ACC2` varchar(100) NOT NULL COMMENT '密碼',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL,
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='車道IP Camera';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecarledccdevice` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL COMMENT 'tb_device_m.Id',
  `CommunityUid` varchar(36) DEFAULT NULL COMMENT '社區識別代號',
  `DefaultWelcomeText` varchar(100) NOT NULL COMMENT '預設顯示歡迎文字',
  `TimeDisplayItems` varchar(100) NOT NULL COMMENT '時間顯示項(以逗號隔開)',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='LED字幕機';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecarlicenserecognition` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `ACC` varchar(100) NOT NULL COMMENT '帳號',
  `ACC2` varchar(100) NOT NULL COMMENT '密碼',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL COMMENT 'tb_device_m.Id',
  `CommunityUid` varchar(36) DEFAULT NULL COMMENT '社區識別代號',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='車牌辨識';",
@"CREATE TABLE IF NOT EXISTS `tb_devicecarxiaomitv` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL COMMENT 'tb_device_m.Id',
  `CommunityUid` varchar(36) DEFAULT NULL COMMENT '社區識別代號',
  `CarDeviceSystemLaneId` varchar(36) DEFAULT NULL COMMENT '車道系統(FK tb_cardevicesystemlane.Id)',
  `EntryType` int NOT NULL DEFAULT '1' COMMENT '出入口(入口:1,出口:2)',
  `CameraCarDeviceMId` varchar(36) DEFAULT NULL COMMENT '監看的車道設備資料代號(FK tb_cardevice_m.Id)',
  `DeviceHumidTempMeterId` varchar(36) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT '溫溼度計Id',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='小米電視盒';",
@"CREATE TABLE IF NOT EXISTS `tb_deviceipcamera` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `ACC` varchar(100) NOT NULL COMMENT '帳號',
  `ACC2` varchar(100) NOT NULL COMMENT '密碼',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Device_M_Id` varchar(36) NOT NULL COMMENT 'tb_device_m.Id',
  `CommunityUid` varchar(36) DEFAULT NULL COMMENT '社區識別代號',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='IP Camera';",
@"ALTER TABLE tb_devicealertlog ADD Type int NOT NULL COMMENT '設備模組種類(0:門禁,1:車道)' DEFAULT 0" ,
@"CREATE TABLE IF NOT EXISTS `tb_communitynoticedevicem` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `Name` varchar(100) NOT NULL COMMENT '位置',
  `Type` varchar(100) NOT NULL COMMENT '設備類型',
  `IP` varchar(100) DEFAULT NULL COMMENT 'IP',
  `Port` varchar(100) DEFAULT NULL COMMENT 'PORT',
  `Remark` varchar(100) DEFAULT NULL COMMENT '備註',
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `Status` int DEFAULT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL COMMENT '建立者',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新者',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `ConnectStatus` int NOT NULL DEFAULT '0' COMMENT '連線狀態 0.離線 1.在線',
  `ConnectErrorTimes` int NOT NULL DEFAULT '0' COMMENT '偵測失敗次數',
  `ConnectException` varchar(5000) DEFAULT NULL COMMENT '偵測錯誤',
  `CommunityNoticeTvDisplaySettingId` varchar(36) DEFAULT NULL COMMENT '社區公告版型(tb_communitynoticetvdisplaysetting.Id)',
  `DeviceHumidTempMeterId` varchar(36) DEFAULT NULL COMMENT '溫溼度計Id',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='公告電視設備資料';",
@"CREATE TABLE IF NOT EXISTS `tb_communitynoticetvdisplaysetting` (
  `Id` varchar(36) NOT NULL COMMENT '唯一識別碼',
  `CommunityNoticeTvDisplaySettingName` varchar(100) DEFAULT NULL COMMENT '版型名稱',
  `CategoryCsv` text COMMENT '分類',
  `ShowHeaderPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示標題區塊',
  `MainHeaderTextHtml` text COMMENT '主標題文字樣式',
  `SubHeaderTextHtml` text COMMENT '時間文字樣式',
  `ShowImagePanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示廣告區',
  `ShowInfoPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示溫度濕度狀態區塊',
  `InfoTextHtml` text COMMENT '溫度濕度狀態文字樣式',
  `ShowAlertPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示通知與警告區塊',
  `AlertTextHtml` text COMMENT '通知與警告區塊文字樣式',
  `ShowNoticeHeaderPanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示公告區塊主標題',
  `ShowNoticePanel` int NOT NULL DEFAULT '0' COMMENT '是否顯示公告內容區塊',
  `NoticeHeaderTextHtml` text COMMENT '公告區塊主標題文字樣式',
  `NoticeTitleTextHtml` text COMMENT '公告標題文字樣式',
  `NoticeContentTextHtml` text COMMENT '公告內容文字樣式',
  `EmergencyDataSql` text COMMENT '緊急事件資料 SQL',
  `NoticeDataSql` text COMMENT '公告資料 SQL',
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `Status` int NOT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL COMMENT '建立人員',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新人員',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `BoardDirection` int NOT NULL COMMENT '看板方向',
  `InfoValueTextHtml` text COMMENT '量測值文字樣式',
  `DisplayMeasurementValueCsv` varchar(200) DEFAULT NULL COMMENT '顯示的量測值',
  `CommunityNameTextHtml` text COMMENT '社區名稱文字樣式',
  `ShowCommunityNamePanel` int DEFAULT NULL COMMENT '是否顯示社區名稱區塊',
  `ImageTransitionType` int DEFAULT NULL COMMENT '廣告轉場效果',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='社區公告版型設定';",
@"CREATE TABLE IF NOT EXISTS `tb_communitynoticetvdisplaysettingad` (
  `Id` varchar(36) NOT NULL COMMENT '唯一識別碼',
  `CommunityNoticeTvDisplaySettingId` varchar(36) NOT NULL COMMENT 'tb_communitynoticetvdisplaysetting.Id',
  `FileType` int NOT NULL COMMENT '檔案類型',
  `FileId` varchar(36) DEFAULT NULL COMMENT '檔案資料代號(tb_file.Id)',
  `FileUrl` varchar(1000) DEFAULT NULL COMMENT '檔案連結',
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `Status` int NOT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL COMMENT '建立人員',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新人員',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  `Seq` int NOT NULL COMMENT '順序',
  `PlaySecond` int DEFAULT NULL COMMENT '播放秒數',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='社區公告版型廣告設定';",
@"ALTER TABLE tb_devicematchingcard ADD RelayClosingTime INT NULL COMMENT '繼電器閉合時間';",
@"ALTER TABLE tb_devicematchingcard ADD ModuleNum INT NULL COMMENT '下掛模組數量';",
@"CREATE TABLE `tb_devicesystempackageroom` ( 
  `Id` varchar(36) NOT NULL, 
  `DeviceAccessId` varchar(36) NOT NULL COMMENT '辨識機', 
  `Creator` varchar(36) NOT NULL COMMENT '建立者', 
  `CreateTime` datetime NOT NULL COMMENT '建立時間', 
  `Updater` varchar(36) NOT NULL COMMENT '更新者', 
  `UpdateTime` datetime NOT NULL COMMENT '更新時間', 
  `Status` int(1) NOT NULL, 
  `DeviceSystemId` varchar(36) NOT NULL, 
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號', 
  PRIMARY KEY (`Id`) 
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='包裹室';",
@"alter table tb_packageroom add DeviceSystemId varchar(36) null comment '包裹室DeviceSystemId'",
@"alter table tb_packageroom add Remark varchar(500) null comment '備註' ",
@"alter table tb_packageroom add PickUpQrCode varchar(36) null comment '取貨qr code';",
@"alter table tb_packageroom add PickUpTime Datetime null comment '取貨時間';",
@"alter table tb_packageroom add PickupDelayEndTime Datetime null comment '取貨延遲時間';",
@"alter table tb_devicesystempackageroom add DeviceQRCodeScannerId  varchar(36) null ;",
@"alter table tb_packageroom add IsScheduleCheck tinyint null comment '排程是否檢查';",
@"alter table tb_devicesystemlocker add DeviceIPCamera1Id varchar(36) null comment 'IP Camera 1';",
@"alter table tb_devicesystemlocker add DeviceIPCamera2Id varchar(36) null comment 'IP Camera 2';",
@"alter table tb_devicelockerinfo add IsReturn tinyint null comment '是否為退貨用 (只能用QrCode開啟櫃門)';",
@"alter table tb_devicelocker add SerialPort varchar(100) null",
@"ALTER TABLE TB_SysRole  ADD LineSetting int(1) ;",
@"ALTER TABLE tb_deviceaccess ADD Day1S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day1E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day2S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day2E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day3S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day3E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day4S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day4E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day5S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day5E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day6S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day6E Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day7S Datetime;",
@"ALTER TABLE tb_deviceaccess ADD Day7E Datetime;",
@"alter table tb_devicelockeriocard add IP varchar(100) null ;",
@"alter table tb_devicelockeriocard add Port varchar(100) null ;",
@"alter table tb_devicelockeriocard add ConnectStatus int null;",
@"alter table tb_devicelockeriocard add ConnectErrorTimes int null;",
@"alter table tb_devicelockeriocard add ConnectException varchar(5000) null;",
@"ALTER TABLE tb_deviceaccess  ADD KeepOpenStatus int(1) ;",
@"CREATE TABLE `tb_linepushsetting` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `CommunityRoomId` varchar(36) NOT NULL COMMENT '門牌ID',
  `Kind` varchar(3) NOT NULL COMMENT '訊息種類',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='門牌管理LINE推播細項設定'
 ",
@"ALTER TABLE tb_devicesystemlocker MODIFY COLUMN DeviceLockerId varchar(36)  NULL COMMENT '信箱/智能櫃';",
@"ALTER TABLE tb_devicelockerinfo ADD DeliveryType INT NULL COMMENT 'CloudPackageTypeEnum 包裹類型  (自助 互動式)';",
@"CREATE TABLE `tb_cartvdisplaysetting` (
  `Id` varchar(36) NOT NULL COMMENT '唯一識別碼',
  `CarTvDisplaySettingName` varchar(100) DEFAULT NULL COMMENT '版型名稱',
  `CategoryCsv` text COMMENT '分類',
  `ShowHeaderPanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示標題區塊',
  `SubHeaderType` int(11) NOT NULL COMMENT '副標題顯示類型',
  `MainHeaderTextHtml` text COMMENT '主標題文字樣式',
  `SubHeaderTextHtml` text COMMENT '副標題文字樣式',
  `ShowImagePanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示車道影像區塊區塊',
  `ShowSignPanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示燈號區塊',
  `SignType` int(11) NOT NULL COMMENT '燈號區塊顯示類型',
  `SignSecondTextHtml` text COMMENT '燈號區塊倒數秒數文字樣式',
  `ShowSignTextPanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示燈號文字訊息區塊',
  `SignTextTextHtml` text COMMENT '燈號文字訊息文字樣式',
  `ShowInfoPanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示溫度濕度狀態區塊',
  `InfoTextHtml` text COMMENT '溫度濕度狀態文字樣式',
  `ShowAlertPanel` int(11) NOT NULL DEFAULT '0' COMMENT '是否顯示通知與警告區塊',
  `AlertTextHtml` text COMMENT '通知與警告區塊文字樣式',
  `EmergencyDataSql` text COMMENT '緊急事件資料 SQL',
  `CommunityUid` varchar(100) DEFAULT NULL COMMENT '社區識別代號',
  `Status` int(11) NOT NULL COMMENT '狀態',
  `Creator` varchar(36) NOT NULL COMMENT '建立人員',
  `CreateTime` datetime NOT NULL COMMENT '建立時間',
  `Updater` varchar(36) NOT NULL COMMENT '更新人員',
  `UpdateTime` datetime NOT NULL COMMENT '更新時間',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='車道電視版型設定';",
@"ALTER TABLE tb_devicelockerinfo ADD PackageNo varchar(100) NULL;",
@"ALTER TABLE tb_devicelockerinfo ADD PackageCommunityRoomId varchar(36) NULL;",
@"ALTER TABLE tb_devicelog ADD PackageFileId varchar(36) NULL COMMENT '包裹照片';",
@"CREATE TABLE `tb_plc_point_mapping` (
  `Id` varchar(36) NOT NULL COMMENT 'ID',
  `CommunityRoomNumber` varchar(10) NOT NULL COMMENT '戶號',
  `PLCAddress` varchar(20) NOT NULL COMMENT 'PLC M點位',
  `Description` varchar(100) DEFAULT NULL COMMENT '點位說明',
  `Creator` varchar(36) NOT NULL,
  `CreateTime` datetime NOT NULL,
  `Updater` varchar(36) NOT NULL,
  `UpdateTime` datetime NOT NULL,
  `Type` varchar(3) DEFAULT '1' COMMENT '0 緊急開關 1 門磁 2 煙感 3 主動紅外 4 被動紅外 11 氣感 99 4.3SOS',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='住戶對應 PLC M 點位表';
 ",
@"ALTER TABLE tb_devicecarXiaomiTV ADD CameraCarDeviceMId varchar(36) NULL COMMENT '監看的車道設備資料代號(FK tb_cardevice_m.Id)';",
@"ALTER TABLE tb_communitynoticedevicem ADD DeviceHumidTempMeterId varchar(36) NULL COMMENT '溫溼度計Id';",
@"ALTER TABLE tb_devicecarXiaomiTV ADD DeviceHumidTempMeterId varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL COMMENT '溫溼度計Id';",
@"ALTER TABLE `tb_devicecarBarrierGate`  ADD COLUMN `Type` INT NOT NULL DEFAULT 1 COMMENT '類別(1:車道系統,2:車道預警系統)' AFTER `SlaveId`;",
@"ALTER TABLE tb_communitynoticetvdisplaysetting ADD InfoValueTextHtml TEXT NULL COMMENT '量測值文字樣式';",
@"ALTER TABLE tb_communitynoticetvdisplaysetting ADD DisplayMeasurementValueCsv varchar(200) NULL COMMENT '顯示的量測值';",
@"ALTER TABLE tb_communitynoticetvdisplaysetting ADD CommunityNameTextHtml TEXT NULL COMMENT '社區名稱文字樣式';",
@"ALTER TABLE tb_communitynoticetvdisplaysetting ADD ShowCommunityNamePanel INT NULL COMMENT '是否顯示社區名稱區塊';",
@"ALTER TABLE tb_cartvdisplaysetting ADD InfoValueTextHtml TEXT NULL COMMENT '量測值文字樣式';",
@"ALTER TABLE tb_cartvdisplaysetting ADD DisplayMeasurementValueCsv varchar(200) NULL COMMENT '顯示的量測值';",
@"ALTER TABLE tb_communitynoticetvdisplaysetting ADD ImageTransitionType INT NULL COMMENT '廣告轉場效果';",
@"ALTER TABLE tb_cartvdisplaysetting ADD MainHeaderText varchar(50) NULL COMMENT '主標題文字';",
@"ALTER TABLE tb_plc_point_mapping MODIFY CommunityRoomNumber VARCHAR(30);",
@"ALTER TABLE tb_publicfacilitiesreservation ADD COLUMN CommunityUserId VARCHAR(36);",
@"ALTER TABLE tb_communitynotice ADD LinePushTarget INT NULL COMMENT '推播對象';",
@"ALTER TABLE tb_communitynotice ADD LinePushCommunityRoomIdCsv TEXT NULL COMMENT '推播門牌';",
@"ALTER TABLE tb_communitynotice ADD LinePushCommunityUserIdCsv TEXT NULL COMMENT '推播住戶';",
@"ALTER TABLE tb_messagepool ADD RelatedDataId varchar(36) NULL COMMENT '關聯資料代號';",
@"ALTER TABLE TB_AccessLog MODIFY COLUMN pic MEDIUMBLOB;",
@"ALTER TABLE tb_write_device_job MODIFY COLUMN JobName varchar(500) NOT NULL COMMENT '工作名稱';",

@"ALTER TABLE tb_deviceaccess ADD COLUMN DoorStatus int null;",
                };
    }
}
