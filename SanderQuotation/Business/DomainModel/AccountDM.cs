namespace Business.DomainModel
{
    public class AccountDM : BaseDM
    {
        public string? MemberAccount { get; set; }
        public string? AccountName { get; set; }
        public Guid? PermissionId { get; set; }
        public string? AccountStatus { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string? MemberPWD { get; set; }
        public DateTime? LastMemberPWDTime { get; set; }
        public string? AccountEmail { get; set; }
        public string? ResetPWDCode { get; set; }
        public DateTime LastForgetPWDTime { get; set; }
        public int Logins { get; set; }
        public DateTime? LockTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        public string LineUserId { get; set; }

        public bool IsEngineer {  get; set; }
        
        /// <summary>
        /// 角色ID列表
        /// </summary>
        public List<string> RoleIds { get; set; } = new List<string>();
        
        /// <summary>
        /// 角色名稱列表
        /// </summary>
        public List<string> RoleNames { get; set; } = new List<string>();
        
        /// <summary>
        /// 角色名稱（用於查詢）
        /// </summary>
        public string? RoleName { get; set; }
        
        /// <summary>
        /// 角色ID（用於查詢）
        /// </summary>
        public Guid RoleId { get; set; }
    }
}