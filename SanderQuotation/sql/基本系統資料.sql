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
	  NEW.flagneedextractkeyword = 1; 
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
VALUES('d8f7638a-beef-40c1-9879-229a2ae20b30', N'admin', N'管理員', NULL, '1', '2025-12-26 11:24:56.000', N'TU+OuPnM0k7DRiECJZCfC9rJ8n56EkdphVpQyLVYcwk=', '2025-01-23 01:43:12.000', N'service@websoft.com.tw', NULL, '2025-01-23 01:43:12.000', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

--系統管理員角色
INSERT INTO TB_SysRole
(Id, RoleName, Status, Memo, LineSetting, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
VALUES('5d28d5da-55de-4ccf-9d17-9e417f8c0c25', N'系統管理員', 1, N'系統管理員', 1, NULL, NULL, '2026-04-30 15:05:00.000', '2026-04-30 16:34:00.000');

--系統管理員與角色綁定
INSERT INTO public.tb_accountsysrole
(id, accountid, roleid, status, createdby, updatedby, createdat, updatedat)
VALUES('74e75d7f-9cbe-4d64-bd67-080ecdfd8ec7'::uuid, 'd8f7638a-beef-40c1-9879-229a2ae20b30'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, NULL, NULL, NULL, '2026-05-11 15:32:51.867', '2026-05-11 15:32:51.867');

--功能類別
INSERT INTO public.tb_sysfuncclass
(id, classname, status, memo, "sequence", createdby, updatedby, createdat, updatedat)
VALUES('13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '系統管理', 1, NULL, 1, NULL, NULL, '2026-05-11 09:43:57.033', '2026-05-11 09:43:57.033');
INSERT INTO public.tb_sysfuncclass
(id, classname, status, memo, "sequence", createdby, updatedby, createdat, updatedat)
VALUES('d1519d1e-df1b-403b-92fc-b210e80815bf'::uuid, '查價轉檔執行模組', 1, NULL, 2, NULL, NULL, '2026-05-11 09:43:57.033', '2026-05-11 09:43:57.033');
INSERT INTO public.tb_sysfuncclass
(id, classname, status, memo, "sequence", createdby, updatedby, createdat, updatedat)
VALUES('13d5da32-cab3-4062-9cf3-ac3e358bc17d'::uuid, '採購管理', 1, NULL, 3, NULL, NULL, '2026-05-11 09:43:57.033', '2026-05-11 09:43:57.033');
INSERT INTO public.tb_sysfuncclass
(id, classname, status, memo, "sequence", createdby, updatedby, createdat, updatedat)
VALUES('28cf91d6-5702-4a28-a1f4-7d57a9e4a15d'::uuid, 'AI資料管理', 1, 'AI資料管理', 4, NULL, NULL, '2026-05-11 09:43:57.033', '2026-05-12 15:28:41.287');

--功能資料
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('ba2da625-4953-4a00-98ee-8bd9b32aae1a'::uuid, '帳號管理', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/Account', '1', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-11 18:06:40.335');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('f0fce558-d449-4fb5-b1e1-752aa475928c'::uuid, '角色管理', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/SysRole', '2', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-11 18:07:32.958');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('2655d82d-a8fc-4d72-a6b5-ad4fc2ed6ded'::uuid, 'Log紀錄', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/ControlLog', '3', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-11 18:07:50.077');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('ec1f492a-ab4f-47cf-94c8-afae5524719b'::uuid, '資料庫設定', 'd1519d1e-df1b-403b-92fc-b210e80815bf'::uuid, '/ESDbTransfer', '1', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-11 18:09:18.490');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('e6df684b-7995-46ec-97b8-54b9afa5ccb7'::uuid, '資料表轉檔設定', 'd1519d1e-df1b-403b-92fc-b210e80815bf'::uuid, '/ESDbTransferMapping', '2', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-11 18:09:42.586');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('019e16b3-73f3-7556-9d75-381f345e76b4'::uuid, '歷史資料上傳', '28cf91d6-5702-4a28-a1f4-7d57a9e4a15d'::uuid, '/HistoryFile', '1', 1, '', NULL, NULL, '2026-05-11 19:01:15.949', '2026-05-11 19:01:15.949');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('019e16b0-6787-72a3-9496-015f3a009cec'::uuid, '轉入檔案上傳', '13d5da32-cab3-4062-9cf3-ac3e358bc17d'::uuid, '/EsFileTransferUpload', '1', 1, '', NULL, NULL, '2026-05-11 18:57:06.024', '2026-05-12 16:29:34.938');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('019e1b51-d8c6-70cf-b94b-9d5ee82ac8ec'::uuid, '定時查價結果', '13d5da32-cab3-4062-9cf3-ac3e358bc17d'::uuid, '/QuotationResult', '2', 1, '', NULL, NULL, '2026-05-12 16:32:48.776', '2026-05-12 16:32:48.776');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('17b62505-604b-443d-a15d-3554fa69357b'::uuid, '排程週期設定', 'd1519d1e-df1b-403b-92fc-b210e80815bf'::uuid, '/CycleSettings', '4', 1, '', NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-13 14:05:46.852');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('019e3938-0feb-7434-88f7-e70ed8dd4174'::uuid, '系統設定', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/SysSetting', '10', 1, '', NULL, NULL, '2026-05-18 11:52:19.537', '2026-05-18 11:56:12.409');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('74fe5247-ab84-44dc-b71e-b00fd851e7e9'::uuid, 'Excel轉入資料表對應設定', 'd1519d1e-df1b-403b-92fc-b210e80815bf'::uuid, '/TableExcel', '3', 1, '', NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-27 13:40:54.821');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('c590e7e4-e502-4b08-a576-bd0c57264c5d'::uuid, '功能類別管理', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/SysFuncClass', '4', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-27 14:01:08.653');
INSERT INTO public.tb_sysfunc
(id, "name", funcclassid, url, "sequence", status, memo, createdby, updatedby, createdat, updatedat)
VALUES('fd2c0fde-58bb-43e7-b834-f797be46d8df'::uuid, '功能管理', '13b37b82-82c6-430e-8dd8-7566dc85377f'::uuid, '/SysFunc', '5', 1, '', NULL, NULL, '2026-05-11 09:50:02.550', '2026-05-27 14:01:21.711');

--功能權限資料
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('abc13e45-cb98-414b-a609-94bbff2df9fa'::uuid, 'N', 'ba2da625-4953-4a00-98ee-8bd9b32aae1a'::uuid, '1', '990001', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('0ccf909a-988e-4f82-a30c-45e6485ffdf2'::uuid, 'N', 'ba2da625-4953-4a00-98ee-8bd9b32aae1a'::uuid, '2', '990002', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('895c3d28-bae7-4ff1-8934-ac076f3a2ae4'::uuid, 'N', 'ba2da625-4953-4a00-98ee-8bd9b32aae1a'::uuid, '3', '990003', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('02876b93-358e-4174-ad68-cf516afdb884'::uuid, 'N', 'ba2da625-4953-4a00-98ee-8bd9b32aae1a'::uuid, '4', '990004', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('7768d0d9-aca6-4681-90fb-f2ac4d409476'::uuid, 'N', 'f0fce558-d449-4fb5-b1e1-752aa475928c'::uuid, '1', '990021', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('c8296055-5694-45da-be29-8293410e09ea'::uuid, 'N', 'f0fce558-d449-4fb5-b1e1-752aa475928c'::uuid, '2', '990022', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('cdaa38ad-b96e-4311-8fc9-c8aa3fa636cb'::uuid, 'N', 'f0fce558-d449-4fb5-b1e1-752aa475928c'::uuid, '3', '990023', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('e3c41c85-09ab-4594-b241-aabbca962050'::uuid, 'N', 'f0fce558-d449-4fb5-b1e1-752aa475928c'::uuid, '4', '990024', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('3b7ea990-7317-4e7a-bc58-c5b5d57b029d'::uuid, 'N', '2655d82d-a8fc-4d72-a6b5-ad4fc2ed6ded'::uuid, '1', '990031', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('146d2b6d-ab0b-4433-a9d4-8c7586bcfaf9'::uuid, 'N', 'ec1f492a-ab4f-47cf-94c8-afae5524719b'::uuid, '1', '990071', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('ccb7c89d-f557-4071-ab28-622de19e3fc9'::uuid, 'N', 'ec1f492a-ab4f-47cf-94c8-afae5524719b'::uuid, '2', '990072', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('076b1fdf-0aad-4329-9cec-17ca3e8d1509'::uuid, 'N', 'ec1f492a-ab4f-47cf-94c8-afae5524719b'::uuid, '3', '990073', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('e3cd5d79-c7a7-4f74-97dc-027c20cf1fa6'::uuid, 'N', 'ec1f492a-ab4f-47cf-94c8-afae5524719b'::uuid, '4', '990074', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('977150f7-b48d-4c03-8c1f-0a11e840afde'::uuid, 'N', 'e6df684b-7995-46ec-97b8-54b9afa5ccb7'::uuid, '1', '990081', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('107cdefd-5cde-4cae-81fa-ebd6854dfc0e'::uuid, 'N', 'e6df684b-7995-46ec-97b8-54b9afa5ccb7'::uuid, '2', '990082', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('1709d49d-6429-44d2-9090-3006d131d61c'::uuid, 'N', 'e6df684b-7995-46ec-97b8-54b9afa5ccb7'::uuid, '3', '990083', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('c0f486f3-9ec1-40d0-93c2-4a409c3d6190'::uuid, 'N', 'e6df684b-7995-46ec-97b8-54b9afa5ccb7'::uuid, '4', '990084', 1, NULL, NULL, '2026-05-11 18:03:12.898', '2026-05-11 18:03:12.898');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('8602a57c-2355-44fe-9209-f1ed57ded34e'::uuid, 'N', '74fe5247-ab84-44dc-b71e-b00fd851e7e9'::uuid, '2', '990202', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('413b7ee4-07d1-4771-8f46-3e935ae0a259'::uuid, 'N', '74fe5247-ab84-44dc-b71e-b00fd851e7e9'::uuid, '3', '990203', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('ca434c1f-ebd8-4a83-865f-8e6dffc4a0ce'::uuid, 'N', '74fe5247-ab84-44dc-b71e-b00fd851e7e9'::uuid, '4', '990204', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b3-7437-70a8-a680-f4830333da00'::uuid, 'N', '019e16b3-73f3-7556-9d75-381f345e76b4'::uuid, '1', '990211', 1, NULL, NULL, '2026-05-11 19:01:15.949', '2026-05-11 19:01:15.949');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b3-7441-733e-b67d-0d443d5af383'::uuid, 'N', '019e16b3-73f3-7556-9d75-381f345e76b4'::uuid, '2', '990212', 1, NULL, NULL, '2026-05-11 19:01:15.949', '2026-05-11 19:01:15.949');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b3-744b-738f-8749-97461d790197'::uuid, 'N', '019e16b3-73f3-7556-9d75-381f345e76b4'::uuid, '3', '990213', 1, NULL, NULL, '2026-05-11 19:01:15.949', '2026-05-11 19:01:15.949');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b3-7451-72b3-a190-bdb314a05497'::uuid, 'N', '019e16b3-73f3-7556-9d75-381f345e76b4'::uuid, '4', '990214', 1, NULL, NULL, '2026-05-11 19:01:15.949', '2026-05-11 19:01:15.949');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b0-67ba-75b8-aaee-4c67747931c1'::uuid, 'N', '019e16b0-6787-72a3-9496-015f3a009cec'::uuid, '1', '990221', 1, NULL, NULL, '2026-05-11 18:57:06.024', '2026-05-11 18:57:06.024');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b0-67bf-72f2-9359-e3941da0dbd5'::uuid, 'N', '019e16b0-6787-72a3-9496-015f3a009cec'::uuid, '2', '990222', 1, NULL, NULL, '2026-05-11 18:57:06.024', '2026-05-11 18:57:06.024');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b0-67c4-76e0-9c23-219274bc947d'::uuid, 'N', '019e16b0-6787-72a3-9496-015f3a009cec'::uuid, '3', '990223', 1, NULL, NULL, '2026-05-11 18:57:06.024', '2026-05-11 18:57:06.024');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e16b0-67c9-721a-9fbb-027a4b27fb78'::uuid, 'N', '019e16b0-6787-72a3-9496-015f3a009cec'::uuid, '4', '990224', 1, NULL, NULL, '2026-05-11 18:57:06.024', '2026-05-11 18:57:06.024');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e1b51-d8e8-76ed-a245-548e81c6eafd'::uuid, 'N', '019e1b51-d8c6-70cf-b94b-9d5ee82ac8ec'::uuid, '1', '40001', 1, NULL, NULL, '2026-05-12 16:32:48.776', '2026-05-12 16:32:48.776');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e1b51-d8ec-72a7-932e-0e8d8c44749f'::uuid, 'N', '019e1b51-d8c6-70cf-b94b-9d5ee82ac8ec'::uuid, '2', '40002', 1, NULL, NULL, '2026-05-12 16:32:48.776', '2026-05-12 16:32:48.776');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e1b51-d8f0-73dd-b81d-7fcdea542cde'::uuid, 'N', '019e1b51-d8c6-70cf-b94b-9d5ee82ac8ec'::uuid, '3', '40003', 1, NULL, NULL, '2026-05-12 16:32:48.776', '2026-05-12 16:32:48.776');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e1b51-d8f4-72c5-bce2-6d021e943368'::uuid, 'N', '019e1b51-d8c6-70cf-b94b-9d5ee82ac8ec'::uuid, '4', '40004', 1, NULL, NULL, '2026-05-12 16:32:48.776', '2026-05-12 16:32:48.776');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('d2c80cef-c678-4ee1-9dcd-82866ecda3f6'::uuid, 'N', '17b62505-604b-443d-a15d-3554fa69357b'::uuid, '1', '990091', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('9e7dfa95-3366-448c-829e-b89c1e8a8894'::uuid, 'N', '17b62505-604b-443d-a15d-3554fa69357b'::uuid, '2', '990092', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('22e28dee-6698-42f0-a751-881a12c7ad23'::uuid, 'N', '17b62505-604b-443d-a15d-3554fa69357b'::uuid, '3', '990093', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('4b5ec198-c7f5-4dc5-a5a6-bba49028cc02'::uuid, 'N', '17b62505-604b-443d-a15d-3554fa69357b'::uuid, '4', '990094', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e3938-102b-73a0-afa4-32f0b48a4819'::uuid, 'N', '019e3938-0feb-7434-88f7-e70ed8dd4174'::uuid, '1', '50001', 1, NULL, NULL, '2026-05-18 11:52:19.537', '2026-05-18 11:52:19.537');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e3938-102f-74a3-b721-178ed84f408b'::uuid, 'N', '019e3938-0feb-7434-88f7-e70ed8dd4174'::uuid, '2', '', 1, NULL, NULL, '2026-05-18 11:52:19.537', '2026-05-18 11:52:19.537');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e3938-1035-7116-bfea-924b18f25b42'::uuid, 'N', '019e3938-0feb-7434-88f7-e70ed8dd4174'::uuid, '3', '', 1, NULL, NULL, '2026-05-18 11:52:19.537', '2026-05-18 11:52:19.537');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019e3938-1039-76b0-ab26-c4c44be1247b'::uuid, 'N', '019e3938-0feb-7434-88f7-e70ed8dd4174'::uuid, '4', '', 1, NULL, NULL, '2026-05-18 11:52:19.537', '2026-05-18 11:52:19.537');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('b5f551cf-6124-4cd6-bccf-5dd4c0ec5f77'::uuid, 'N', '74fe5247-ab84-44dc-b71e-b00fd851e7e9'::uuid, '1', '990201', 1, NULL, NULL, '2026-05-11 18:31:46.907', '2026-05-11 18:31:46.907');

--角色與權限綁定資料
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9f89-72a9-8064-ae2405ac2508'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b3-7437-70a8-a680-f4830333da00'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fb3-77ee-95b8-46420a5e3bb0'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b3-744b-738f-8749-97461d790197'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fc5-7121-9b3d-1110d7e8cbff'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e1b51-d8e8-76ed-a245-548e81c6eafd'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fcc-7781-b988-cb0c42838991'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e1b51-d8f0-73dd-b81d-7fcdea542cde'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fd4-76c8-ac2d-9314e607f36f'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b0-67ba-75b8-aaee-4c67747931c1'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fdc-7204-8ec6-65426f405319'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b0-67c4-76e0-9c23-219274bc947d'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fe8-7440-9a4f-8eefa5b57eec'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'b5f551cf-6124-4cd6-bccf-5dd4c0ec5f77'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fee-7254-b4dd-cd9895e2338c'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '413b7ee4-07d1-4771-8f46-3e935ae0a259'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9ff4-76fa-8b83-12794f259a00'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'd2c80cef-c678-4ee1-9dcd-82866ecda3f6'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9ffb-75ca-8014-3ba94671f351'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '22e28dee-6698-42f0-a751-881a12c7ad23'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a001-76c0-881d-bd4e8d01bab8'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '146d2b6d-ab0b-4433-a9d4-8c7586bcfaf9'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a007-77d4-ae29-e5477bd21a8a'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '076b1fdf-0aad-4329-9cec-17ca3e8d1509'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a010-7206-a088-f056964e40cb'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '977150f7-b48d-4c03-8c1f-0a11e840afde'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a015-70e0-a146-1bc338fe6ec8'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '1709d49d-6429-44d2-9090-3006d131d61c'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a024-7164-a050-b5c01d534150'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '3b7ea990-7317-4e7a-bc58-c5b5d57b029d'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a048-7015-9908-304b617bfa21'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '0ccf909a-988e-4f82-a30c-45e6485ffdf2'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a065-7392-9cd7-d0ee0c7beecf'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '02876b93-358e-4174-ad68-cf516afdb884'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a094-7757-a387-33f8851de0ee'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '7768d0d9-aca6-4681-90fb-f2ac4d409476'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a0ad-72f8-8c41-49e72b51bf17'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'cdaa38ad-b96e-4311-8fc9-c8aa3fa636cb'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fae-7748-ae55-d6d217d4b53f'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b3-7441-733e-b67d-0d443d5af383'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fbf-74a9-bb19-a2b7f8077bbc'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b3-7451-72b3-a190-bdb314a05497'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fc9-7419-a304-78bf784c4478'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e1b51-d8ec-72a7-932e-0e8d8c44749f'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fcf-7430-867b-5604dce4473f'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e1b51-d8f4-72c5-bce2-6d021e943368'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fd9-74a6-8722-13c2221b2503'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b0-67bf-72f2-9359-e3941da0dbd5'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9fe5-76ba-be2d-6c3792693973'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e16b0-67c9-721a-9fbb-027a4b27fb78'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9feb-7313-8e7c-f664b3530c35'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '8602a57c-2355-44fe-9209-f1ed57ded34e'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9ff1-7455-8770-acb1605bc13a'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'ca434c1f-ebd8-4a83-865f-8e6dffc4a0ce'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9ff8-7269-8050-56e12e86ab89'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '9e7dfa95-3366-448c-829e-b89c1e8a8894'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-9ffe-72ae-9dc7-dd70ac650447'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '4b5ec198-c7f5-4dc5-a5a6-bba49028cc02'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a004-75e7-a59e-facba6b3257e'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'ccb7c89d-f557-4071-ab28-622de19e3fc9'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a00d-7078-bea5-b717781b866f'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'e3cd5d79-c7a7-4f74-97dc-027c20cf1fa6'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a013-74ef-a05f-9fcc9fae40a0'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '107cdefd-5cde-4cae-81fa-ebd6854dfc0e'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a018-72fc-a2b2-3f4a4a2b8914'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'c0f486f3-9ec1-40d0-93c2-4a409c3d6190'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a02a-74ac-9538-9d5153e6a1ae'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'abc13e45-cb98-414b-a609-94bbff2df9fa'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a053-774e-9916-8223dee7a6a4'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '895c3d28-bae7-4ff1-8934-ac076f3a2ae4'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a076-7100-a2cd-512f07aa9244'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019e3938-102b-73a0-afa4-32f0b48a4819'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a0a1-71da-aacb-af7f987224b1'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'c8296055-5694-45da-be29-8293410e09ea'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019e6806-a0b7-75d5-b449-bd44227b09ed'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, 'e3c41c85-09ab-4594-b241-aabbca962050'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');

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
-- 系統排程設定
INSERT INTO esschedulecycle
(schedulecyclecode, "type", sortno, priority, createdat, updatedat, createdby, updatedby, cyclename, description, cycletype, cronexpression, secondinterval, minuteinterval, minuteatsecond, hourinterval, houratminute, houratsecond, dayinterval, dayattime, weekattime, monthattime, lastrunat, lastrunstatus, lastrunmessage, id, status)
VALUES('SYS1', '1', '', ' ', '2026-05-28 13:52:44.617', '2026-05-28 14:24:40.995', NULL, 'admin', '內部料品表 AI 解析', '', 'DAY', '0 0 2 */1 * *', 1, 1, NULL, 1, NULL, NULL, 1, '02:00', '08:00', '08:00', NULL, NULL, NULL, '23854124-4470-446b-aae2-c55aeaf12097'::uuid, 1);
INSERT INTO esschedulecycle
(schedulecyclecode, "type", sortno, priority, createdat, updatedat, createdby, updatedby, cyclename, description, cycletype, cronexpression, secondinterval, minuteinterval, minuteatsecond, hourinterval, houratminute, houratsecond, dayinterval, dayattime, weekattime, monthattime, lastrunat, lastrunstatus, lastrunmessage, id, status)
VALUES('SYS2', '1', '', ' ', '2026-05-28 13:52:44.617', '2026-05-28 14:25:34.789', NULL, 'admin', '查價', '', 'MINUTE', '0 */10 * * * *', 1, 10, NULL, 1, NULL, NULL, 1, '08:00', '08:00', '08:00', NULL, NULL, NULL, '5fe85cc4-c165-43c6-82ce-693be77b0368'::uuid, 1);
INSERT INTO esschedulecycleothertransfer
(id, schedulecyclecode, actiontype, "type", sortno, priority, createdat, updatedat, createdby, updatedby, status)
VALUES('a0df14bb-02bb-48d8-a040-27afc0c7bc59'::uuid, 'SYS2', 1, NULL, '', ' ', '2026-05-28 14:25:34.789', '2026-05-28 14:25:34.789', 'admin', 'admin', 1);
INSERT INTO esschedulecycleothertransfer
(id, schedulecyclecode, actiontype, "type", sortno, priority, createdat, updatedat, createdby, updatedby, status)
VALUES('043774b6-073c-4232-9dae-7d6519d5ff1b'::uuid, 'SYS1', 2, NULL, '', ' ', '2026-05-28 14:24:40.995', '2026-05-28 14:24:40.995', 'admin', 'admin', 1);
