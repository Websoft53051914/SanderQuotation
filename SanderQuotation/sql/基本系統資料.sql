-- gen_random_uuid() 
CREATE EXTENSION IF NOT EXISTS pgcrypto;
-- 模糊查詢
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Function
-- 更新 updatedat 欄位
CREATE OR REPLACE FUNCTION trg_update_column_updatedat()
RETURNS TRIGGER AS
$$
BEGIN
-- 更新 updatedat 欄位
    NEW.updatedat = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';
-- 檢查 sandermoduleitem 規格內容是否有更新
CREATE OR REPLACE FUNCTION trg_sandermoduleitem_check_update() RETURNS TRIGGER AS 
$$ 
BEGIN
-- 檢查 sandermoduleitem 規格內容是否有更新
	IF OLD.description IS DISTINCT FROM NEW.description
	   OR OLD.description2 IS DISTINCT FROM NEW.description2
	   OR OLD.longdesc IS DISTINCT FROM NEW.longdesc
	   OR OLD.longdesc2 IS DISTINCT FROM NEW.longdesc2 
	THEN 
	  NEW.flagneedextractkeyword = true; 
	END IF;
	RETURN NEW; 
END; 
$$ LANGUAGE plpgsql;
-- 檢查 sandermoduleitemvariant 規格內容是否有更新
CREATE OR REPLACE FUNCTION trg_sandermoduleitemvariant_check_update() RETURNS TRIGGER AS 
$$ 
BEGIN
-- 檢查 sandermoduleitemvariant 規格內容是否有更新
    IF OLD.description IS DISTINCT FROM NEW.description
       OR OLD.description2 IS DISTINCT FROM NEW.description2 
    THEN 
      NEW.flagneedextractkeyword = 1; 
    END IF;
    RETURN NEW; 
END; 
$$ LANGUAGE plpgsql;

-- Table Schema

-- 

-- Data
--系統管理員帳號
INSERT INTO TB_Account
(Id, MemberAccount, AccountName, PermissionId, AccountStatus, LastLoginTime, MemberPWD, LastMemberPWDTime, AccountEmail, ResetPWDCode, LastForgetPWDTime, Logins, LockTime, LogoutTime, Status, LineUserId, Type, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
VALUES(gen_random_uuid(), N'admin', N'管理員', NULL, '1', '2025-12-26 11:24:56.000', N'TU+OuPnM0k7DRiECJZCfC9rJ8n56EkdphVpQyLVYcwk=', '2025-01-23 01:43:12.000', N'service@websoft.com.tw', NULL, '2025-01-23 01:43:12.000', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

--系統管理員角色
INSERT INTO TB_SysRole
(Id, RoleName, Status, Memo, LineSetting, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
VALUES(gen_random_uuid(), N'系統管理員', 1, N'系統管理員', 1, NULL, NULL, '2026-04-30 15:05:00.000', '2026-04-30 16:34:00.000');

--系統設定預設值
INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'ARROW', 'ARROW', 1, 'PreferredVendorList', '', '', now(), now());
INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'FUTURE', 'FUTURE', 1, 'PreferredVendorList', '', '', now(), now());
INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'TTI', 'TTI', 1, 'PreferredVendorList', '', '', now(), now());
INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'DIGIKEY', 'DIGIKEY', 1, 'PreferredVendorList', '', '', now(), now());
INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'MOUSER', 'MOUSER', 1, 'PreferredVendorList', '', '', now(), now());


INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), 'DIODE', 'DIODE', 1, 'BrandComparisonCategoryList', '', '', now(), now());

INSERT INTO public.tb_syssetting
(id, param, value, status, "type", createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), '1', '1', 1, 'AIDecisionProcessDisplaySwitch', '', '', now(), now());
