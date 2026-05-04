using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("TB_Account")]
    public class TB_AccountEntity : SP_BaseEntity
    {
        public string? MemberAccount { get; set; }
        public string? AccountName { get; set; }
        public int? PermissionId { get; set; }
        public string? AccountStatus { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string? MemberPWD { get; set; }
        public DateTime? LastMemberPWDTime { get; set; }
        public string? AccountEmail { get; set; }
        public string? ResetPwdCode { get; set; }
        public DateTime? LastForgetPWDTime { get; set; }
        public int Logins { get; set; }
        public DateTime? LockTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        //public string? LineUserId { get; set; }

    }
}
