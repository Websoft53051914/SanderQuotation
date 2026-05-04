

namespace Business.DomainModel
{
    public partial class MemberDM : BaseDM
    {
        public string? MemberAccount { get; set; }
        public string? AccountName { get; set; }
        public int? PermissionId { get; set; }
        public string? AccountStatus { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string? MemberPWD { get; set; }
        public DateTime? LastMemberPWDTime { get; set; }
        public string? AccountEmail { get; set; }
        public string? ResetPwdCode { get; set; }
        public DateTime LastForgetPWDTime { get; set; }
        public int Logins { get; set; }
        public DateTime? LockTime { get; set; }
        public DateTime? LogoutTime { get; set; }

    }

    public partial class MemberDM 
    {
        public List<string>? PermissionIDs { get; set; }
        public string? RoleName { get; set; }
        public Guid RoleId { get; set; }
        public string? TESTA { get; set; }
        public string? TESTAC { get; set; }

    }
}
