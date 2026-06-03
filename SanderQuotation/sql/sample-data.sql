-- 內部料品表
CREATE TABLE ai_module_item_vw (
    no_                 VARCHAR(20)  NOT NULL,
    item_category_code  VARCHAR(10)  NOT NULL,
    long_desc_          VARCHAR(250) NOT NULL,
    long_desc_2         VARCHAR(250) NOT NULL,
    description         VARCHAR(50)  NOT NULL,
    description_2       VARCHAR(50)  NOT NULL
);

-- Table Comment
COMMENT ON TABLE ai_module_item_vw IS '內部料品表(範例)';

-- Column Comments
COMMENT ON COLUMN ai_module_item_vw.no_
IS '內部料號';

COMMENT ON COLUMN ai_module_item_vw.item_category_code
IS '判斷是否需比對廠牌之類別代碼';

COMMENT ON COLUMN ai_module_item_vw.long_desc_
IS 'MPN 主要欄位（第一優先）';

COMMENT ON COLUMN ai_module_item_vw.long_desc_2
IS 'MPN 次要欄位 + 自由文字備註';

COMMENT ON COLUMN ai_module_item_vw.description
IS '料品規格主欄位';

COMMENT ON COLUMN ai_module_item_vw.description_2
IS '料品規格次欄位';


INSERT INTO esdbtransfer
(transfercode, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfername, dbtype, dbhost, dbport, dbname, dbuser, dbpassword, description)
VALUES('LocalSystem', '019e1a9f-02c4-723e-8f6c-4c1b57f8ff25'::uuid, 1, NULL, NULL, NULL, '2026-05-12 13:18:09.091', '2026-05-12 14:13:18.647', 'admin', NULL, '本地', '1', '192.168.21.27', NULL, 'sander', 'postgres', 'sander@123', '本地資料庫');

INSERT INTO esdbtransfermapping
(transfermappingcode, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, srcdbtransfercode, srctablename, dstdbtransfercode, dsttablename, description, filtercondition, filtermode)
VALUES('範例A', '019e862d-cc2f-7564-8b68-c28736c6ee5e'::uuid, 1, NULL, NULL, NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', 'LocalSystem', 'ai_module_item_vw', 'LocalSystem', 'sandermoduleitem', '範例', '{"mode":"builder","sql":"","rows":[]}', 'builder');
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc32-7358-b943-8f2e110e75f6'::uuid, 1, NULL, 'S0001', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'no_', 'no', false, true);
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc32-7359-9cd6-455abc24f94a'::uuid, 1, NULL, 'S0002', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'item_category_code', 'itemcategorycode', false, false);
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc33-71ec-b7ad-d27bf47e92c9'::uuid, 1, NULL, 'S0003', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'long_desc_', 'longdesc', false, false);
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc33-71ed-ad42-2650675535bb'::uuid, 1, NULL, 'S0004', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'long_desc_2', 'longdesc2', false, false);
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc33-71ee-96b8-442d5ef26305'::uuid, 1, NULL, 'S0005', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'description', 'description', false, false);
INSERT INTO esdbtransfermappingcolumn
(id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srccolumnname, dstcolumnname, isencrypt, isprimarykey)
VALUES('019e862d-cc33-71ef-bb70-028f7a1b30c9'::uuid, 1, NULL, 'S0006', NULL, '2026-06-02 10:33:28.879', '2026-06-02 10:33:28.879', 'admin', 'admin', '範例A', 'description_2', 'description2', false, false);


INSERT INTO esfiletransfermapping
(transfermappingcode, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, examplefilename, examplefiletype, srcnasfilepath, description, filename)
VALUES('範例BOMA', '019e433d-519c-74e4-a302-71467892724f'::uuid, 1, NULL, NULL, NULL, '2026-05-20 10:35:52.600', NULL, 'admin', NULL, 'POC_BOM_A', 1, NULL, 'BOMA 範例', 'POC_BOM_A.xlsx');
INSERT INTO esfiletransfermapping
(transfermappingcode, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, examplefilename, examplefiletype, srcnasfilepath, description, filename)
VALUES('範例BOMB', '019e49ad-603f-71d1-b8cc-6afa8ffb7910'::uuid, 1, NULL, NULL, NULL, '2026-05-21 16:35:59.675', NULL, 'admin', NULL, 'POC_BOM_B', 1, NULL, 'BOMB 範例', 'POC_BOM_B.xlsx');


INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('FEDA40ADF57B4D489BF11421394161F6', '019e433d-51a6-75e5-ba01-25c8d5bd23b1'::uuid, 1, NULL, '1', NULL, '2026-05-20 10:35:52.613', NULL, 'admin', NULL, '範例BOMA', '原始檔', 0, 2, 'bomfilecontent', 'Description', 'description', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('B8806120867A40089855095B54F834F6', '019e433d-51a7-7318-a1da-640c37f0de13'::uuid, 1, NULL, '2', NULL, '2026-05-20 10:35:52.615', NULL, 'admin', NULL, '範例BOMA', '原始檔', 0, 2, 'bomfilecontent', 'Manufacturer 1', 'manufacturer', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('2FFB7E1688D74F73B986332729A2D55E', '019e433d-51a8-70a4-8b7c-54883f72a534'::uuid, 1, NULL, '3', NULL, '2026-05-20 10:35:52.616', NULL, 'admin', NULL, '範例BOMA', '原始檔', 0, 2, 'bomfilecontent', 'Manufacturer Part Number 1', 'manufacturerpartnumber', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('12F6F2A3A8FD4E22A6D1EC4B341DD674', '019e433d-51a8-70a5-b4ee-d43216d022cb'::uuid, 1, NULL, '4', NULL, '2026-05-20 10:35:52.616', NULL, 'admin', NULL, '範例BOMA', '原始檔', 0, 2, 'bomfilecontent', 'Quantity', 'qty', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);

INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('F919C4C636C740B190DD7C4CA553D043', '019e49ad-6048-752b-9692-6990f2c97412'::uuid, 1, NULL, '1', NULL, '2026-05-21 16:35:59.688', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Component Part', 'componentpart', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('1936595BC9C44332B618D8E8B3A4CF8F', '019e49ad-604a-7112-b594-7db37fb3fddc'::uuid, 1, NULL, '2', NULL, '2026-05-21 16:35:59.690', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Description', 'description', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('CC5FB16E93704D6BA64D4458794DAA9A', '019e49ad-604c-77d8-b1b9-5e9b50613737'::uuid, 1, NULL, '3', NULL, '2026-05-21 16:35:59.692', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Qty', 'qty', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('07EE09EC4BD44C89950CEFADE74815E7', '019e49ad-604c-77d9-9be0-4d97124d7d3f'::uuid, 1, NULL, '4', NULL, '2026-05-21 16:35:59.692', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Manufacturer', 'manufacturer', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('08416DAE81ED4AC8AD352508DD9AADF9', '019e49ad-604c-77da-91c6-1845f2370b3d'::uuid, 1, NULL, '5', NULL, '2026-05-21 16:35:59.692', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Manufacturer Part Number', 'manufacturerpartnumber', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);
INSERT INTO esfiletransfermappingcolumn
(esfiletransfermappingcolumnid, id, status, "type", sortno, priority, createdat, updatedat, createdby, updatedby, transfermappingcode, srcsheetname, srcsheetindex, headerrowindex, targettablename, srcfilecolumnname, targettablecolumnname, dbtransfermappingcode, filtercondition, filtermode, defaultvalue, isencrypt, isprimarykey, targettablenamecomment)
VALUES('4C3D62FF6E5847F1B5B4632F72B77940', '019e49ad-604c-77db-b4e7-feebc9dce5dd'::uuid, 1, NULL, '6', NULL, '2026-05-21 16:35:59.692', NULL, 'admin', NULL, '範例BOMB', '原始檔', 0, 4, 'bomfilecontent', 'Display Part', 'displaypart', 'LocalSystem', '{"mode":"builder","sql":"","rows":[]}', 'builder', NULL, false, false, NULL);