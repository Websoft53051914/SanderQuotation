
using Const;
using static Const.Enums;

namespace frontend.Common
{
    public partial class SessionVO
    {
        public int LineSetting { get; set; }
        public string Locale { get; set; } = LocaleConst.ZH_TW;
        public Guid AccountId { get; set; }

        public string? Account { get; set; }
        public string? AccountName { get; set; }
        public List<int>? Roles { get; set; }


        public DateTime LastMemberPWDTime { get; set; }

        //public IEnumerable<PermissionDM> Functions { get; set; }

        public string TempAccount { get; set; }
        public string IP { get; set; }

        /// <summary>
        /// 社區識別代號
        /// </summary>
        public string? CommunityUid { get; set; }
    }


    public partial class SessionVO
    {
        /// <summary>
        /// Locker Device Id
        /// </summary>
        public Guid? DeviceId { get; set; }
        /// <summary>
        /// 自助投遞 step1 輸入正確手機號碼後 紀錄要投遞哪個住戶
        /// </summary>
        public Guid? CoummunityUserId { get; set; }

        public Guid? CoummunityRoomId { get; set; }

        /// <summary>
        /// 偵測的 DI Id
        /// </summary>
        public Guid? DetectDIId { get; set; }

        public Guid? LockerInfoId { get; set; }

        public List<Guid> LockerInfoIds { get; set; }

        /// <summary>
        /// 是否投遞完成
        /// </summary>
        public bool? IsDelivery { get; set; }

        /// <summary>
        /// 地端是否紀錄完成
        /// </summary>
        public bool? IsSiteDone { get; set; }

        public Guid? PickupQRCode { get; set; } // 用於包裹取物的 QRCode Id

        public string PackageNo { get; set; }

        /// <summary>
        /// cloudlockeraction id  Action = 完成
        /// </summary>
        public Guid? TargetId { get; set; }
    }
}
