namespace ViewModel
{
    public partial class AccountVM : BaseVM
    {
        public string? MemberAccount { get; set; }
        public string? AccountName { get; set; }
        public string? AccountEmail { get; set; }
        public string? AccountStatus { get; set; }
        public string? AccountStatusName { get; set; }
        public string? MemberPWD { get; set; }
        public string? ConfirmPassword { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime? LastMemberPWDTime { get; set; }
        public int Logins { get; set; }
        public DateTime? LockTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        
        /// <summary>
        /// 角色ID列表
        /// </summary>
        public List<string> RoleIds { get; set; } = new List<string>();
        
        /// <summary>
        /// 角色名稱列表
        /// </summary>
        public List<string> RoleNames { get; set; } = new List<string>();
        
        /// <summary>
        /// 角色名稱字串（用於顯示）
        /// </summary>
        public string? RoleName { get; set; }
    }
}
