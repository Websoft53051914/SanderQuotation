--系統管理員帳號
INSERT INTO TB_Account
(Id, MemberAccount, AccountName, PermissionId, AccountStatus, LastLoginTime, MemberPWD, LastMemberPWDTime, AccountEmail, ResetPWDCode, LastForgetPWDTime, Logins, LockTime, LogoutTime, Status, LineUserId, Type, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
VALUES(gen_random_uuid(), N'admin', N'管理員', NULL, '1', '2025-12-26 11:24:56.000', N'TU+OuPnM0k7DRiECJZCfC9rJ8n56EkdphVpQyLVYcwk=', '2025-01-23 01:43:12.000', N'service@websoft.com.tw', NULL, '2025-01-23 01:43:12.000', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

--系統管理員角色
INSERT INTO TB_SysRole
(Id, RoleName, Status, Memo, LineSetting, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
VALUES(gen_random_uuid(), N'系統管理員', 1, N'系統管理員', 1, NULL, NULL, '2026-04-30 15:05:00.000', '2026-04-30 16:34:00.000');