using System.ComponentModel;

namespace Business.DomainModel
{
    public class LoginDM
    {
        public Guid Id { set; get; }

        public Guid? EngineerId { set; get; }

        /// <summary>
        /// 會員帳號
        /// </summary>
        [Description("會員帳號")]
        public string MemberAccount { set; get; }

        /// <summary>
        /// 帳號名稱
        /// </summary>
        public string AccountName { set; get; }

        ///// <summary>
        ///// 角色權限FK
        ///// </summary>
        //public int PermissionID { set; get; }

        ///// <summary>
        ///// 帳號狀態，1:啟用 2:停用 3:開通中
        ///// </summary>
        public string AccountStatus { set; get; }

        ///// <summary>
        ///// 最後登入時間
        ///// </summary>
        //public string LastLoginTime { set; get; }
        //[Description("密碼")]
        public string MemberPWD { set; get; }
        public string ConFirmMemberPWD { set; get; }

        /// <summary>
        /// 驗證碼
        /// </summary>
        [Description("圖形驗證碼")]
        public string CaptchaCode { set; get; }

        /// <summary>
        /// 驗證碼的答案
        /// </summary>
        public string CaptchaCodeAnswer { set; get; }

        public List<PermissionDM> Functions { get; set; }

        //public string LoginDateTime { set; get; }
        public string LastMemberPWDTime { set; get; }
        ////public int LoginTime { set; get; }
        public string AccountEMail { set; get; }
        public string ResetPWDCode { set; get; }
        public string LastForgetPWDTime { set; get; }



        public int LineSetting { get; set; }
        public string TempAccount { get; set; }
        public string? IP { get; set; }
        public int Logins { set; get; }
        public string LockTime { set; get; }
    }

    public static class Extensions // CS1106  
    {
        public static string GetMemberDescription<T>(this T t, string memberName) where T : class
        {
            var memberInfo = t.GetType().GetMember(memberName)[0];
            var descriptionAttribute = memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)[0] as DescriptionAttribute;
            return descriptionAttribute.Description;
        }
    }
}
