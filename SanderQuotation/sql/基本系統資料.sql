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
-- public.ailog definition

-- Drop table

-- DROP TABLE public.ailog;

CREATE TABLE public.ailog (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	logtime timestamp NOT NULL,
	account varchar(100) NULL,
	"role" int4 NOT NULL,
	"content" text NULL,
	"sql" text NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	"function" varchar(100) NULL,
	CONSTRAINT pk_ailog PRIMARY KEY (id)
);


-- public.bomfilecontent definition

-- Drop table

-- DROP TABLE public.bomfilecontent;

CREATE TABLE public.bomfilecontent (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	componentpart text NULL,
	description text NULL,
	qty int4 NULL,
	manufacturer text NULL,
	manufacturerpartnumber text NULL,
	displaypart text NULL,
	uploadid uuid NOT NULL,
	createdat timestamp DEFAULT now() NOT NULL,
	updatedat timestamp DEFAULT now() NOT NULL
);

-- Table Triggers

create trigger trg_bomfilecontent_bu_updatedat before
update
    on
    public.bomfilecontent for each row execute function trg_update_column_updatedat();


-- public.embeddedhistoryfile definition

-- Drop table

-- DROP TABLE public.embeddedhistoryfile;

CREATE TABLE public.embeddedhistoryfile (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	embedding public.vector NULL,
	status int4 NULL,
	historyfileid uuid NOT NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT embeddedhistoryfile_pk PRIMARY KEY (id)
);


-- public.esdbtransfer definition

-- Drop table

-- DROP TABLE public.esdbtransfer;

CREATE TABLE public.esdbtransfer (
	transfercode varchar(100) NOT NULL,
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	"type" varchar(50) NULL,
	sortno varchar(50) NULL,
	priority varchar(50) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	transfername varchar(200) NULL,
	dbtype varchar(50) NULL,
	dbhost varchar(200) NULL,
	dbport varchar(20) NULL,
	dbname varchar(200) NULL,
	dbuser varchar(100) NULL,
	dbpassword varchar(200) NULL,
	description text NULL,
	CONSTRAINT esdbtransfer_pk PRIMARY KEY (id)
);


-- public.esdbtransfermapping definition

-- Drop table

-- DROP TABLE public.esdbtransfermapping;

CREATE TABLE public.esdbtransfermapping (
	transfermappingcode varchar(100) NOT NULL,
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	"type" varchar(50) NULL,
	sortno varchar(50) NULL,
	priority varchar(50) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	srcdbtransfercode varchar(100) NOT NULL,
	srctablename varchar(200) NOT NULL,
	dstdbtransfercode varchar(100) NOT NULL,
	dsttablename varchar(200) NOT NULL,
	description text NULL,
	filtercondition text NULL,
	filtermode varchar(50) NULL,
	CONSTRAINT esdbtransfermapping_pk PRIMARY KEY (id)
);


-- public.esdbtransfermappingcolumn definition

-- Drop table

-- DROP TABLE public.esdbtransfermappingcolumn;

CREATE TABLE public.esdbtransfermappingcolumn (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	"type" varchar(50) NULL,
	sortno varchar(50) NULL,
	priority varchar(50) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	transfermappingcode varchar(100) NULL,
	srccolumnname varchar(200) NOT NULL,
	dstcolumnname varchar(200) NOT NULL,
	isencrypt bool NULL,
	isprimarykey bool NULL,
	CONSTRAINT pk_esdbtransfermappingcolumn PRIMARY KEY (id)
);


-- public.esfiletransfermapping definition

-- Drop table

-- DROP TABLE public.esfiletransfermapping;

CREATE TABLE public.esfiletransfermapping (
	transfermappingcode varchar(50) NOT NULL,
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	"type" varchar(50) NULL,
	sortno varchar(50) NULL,
	priority varchar(50) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	examplefilename varchar(255) NULL,
	examplefiletype int4 NOT NULL,
	srcnasfilepath varchar(500) NULL,
	description text NULL,
	filename varchar(100) NULL,
	CONSTRAINT esfiletransfermapping_pk PRIMARY KEY (id)
);


-- public.esfiletransfermappingcolumn definition

-- Drop table

-- DROP TABLE public.esfiletransfermappingcolumn;

CREATE TABLE public.esfiletransfermappingcolumn (
	esfiletransfermappingcolumnid varchar(50) NOT NULL,
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	"type" varchar(50) NULL,
	sortno varchar(50) NULL,
	priority varchar(50) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	transfermappingcode varchar(50) NOT NULL,
	srcsheetname varchar(50) NULL,
	srcsheetindex int4 NOT NULL,
	headerrowindex int4 NOT NULL,
	targettablename varchar(100) NULL,
	srcfilecolumnname varchar(100) NULL,
	targettablecolumnname varchar(100) NULL,
	dbtransfermappingcode varchar(100) NULL,
	filtercondition text NULL,
	filtermode varchar(50) NULL,
	defaultvalue text NULL,
	isencrypt bool NULL,
	isprimarykey bool NULL,
	targettablenamecomment varchar(100) NULL,
	CONSTRAINT esfiletransfermappingcolumn_pk PRIMARY KEY (id)
);


-- public.esfiletransferupload definition

-- Drop table

-- DROP TABLE public.esfiletransferupload;

CREATE TABLE public.esfiletransferupload (
	id uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	uploadid uuid NOT NULL,
	filename varchar(1000) NOT NULL,
	quotationqty int4 NULL,
	customercode varchar(1000) NULL,
	prodno varchar(1000) NULL,
	processstatus int4 NULL,
	esfiletransfermappingid uuid NULL,
	manualcustomername varchar(50) NULL,
	CONSTRAINT esfiletransferupload_pk PRIMARY KEY (id)
);


-- public.esschedulecycle definition

-- Drop table

-- DROP TABLE public.esschedulecycle;

CREATE TABLE public.esschedulecycle (
	schedulecyclecode varchar(50) NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	cyclename varchar(100) NOT NULL,
	description varchar(500) NULL,
	cycletype varchar(20) NOT NULL,
	cronexpression varchar(100) NOT NULL,
	secondinterval int4 NULL,
	minuteinterval int4 NULL,
	minuteatsecond int4 NULL,
	hourinterval int4 NULL,
	houratminute int4 NULL,
	houratsecond int4 NULL,
	dayinterval int4 NULL,
	dayattime bpchar(5) NULL,
	weekattime bpchar(5) NULL,
	monthattime bpchar(5) NULL,
	lastrunat timestamp NULL,
	lastrunstatus varchar(20) NULL,
	lastrunmessage varchar(1000) NULL,
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	CONSTRAINT esschedulecycle_pk PRIMARY KEY (id),
	CONSTRAINT esschedulecycle_unique UNIQUE (schedulecyclecode)
);


-- public.historyfile definition

-- Drop table

-- DROP TABLE public.historyfile;

CREATE TABLE public.historyfile (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	filename varchar(100) NOT NULL,
	uploadid varchar(36) NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	filesummary text NULL,
	CONSTRAINT pk_historyfile PRIMARY KEY (id)
);


-- public.reportitemcustomer definition

-- Drop table

-- DROP TABLE public.reportitemcustomer;

CREATE TABLE public.reportitemcustomer (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	variantcode varchar NULL,
	customercode varchar NULL,
	customername varchar NULL,
	createdat timestamp DEFAULT now() NOT NULL,
	updatedat timestamp DEFAULT now() NOT NULL,
	CONSTRAINT report_item_customer_pkey PRIMARY KEY (id)
);

-- Table Triggers

create trigger trg_reportitemcustomer_bu_updatedat before
update
    on
    public.reportitemcustomer for each row execute function trg_update_column_updatedat();


-- public.sandermoduleitem definition

-- Drop table

-- DROP TABLE public.sandermoduleitem;

CREATE TABLE public.sandermoduleitem (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	"no" varchar(20) NULL,
	description varchar(50) NULL,
	description2 varchar(50) NULL,
	longdesc varchar(250) NULL,
	longdesc2 varchar(250) NULL,
	createdat timestamp DEFAULT now() NOT NULL,
	updatedat timestamp DEFAULT now() NOT NULL,
	itemcategorycode varchar(10) NULL,
	flagneedextractkeyword int4 DEFAULT 1 NOT NULL,
	CONSTRAINT sandermodule_item_pk PRIMARY KEY (id)
);

-- Table Triggers

create trigger trg_sandermoduleitem_bu_updatedat before
update
    on
    public.sandermoduleitem for each row execute function trg_update_column_updatedat();
create trigger trg_sandermoduleitem_bu_isupdate before
update
    on
    public.sandermoduleitem for each row execute function trg_sandermoduleitem_check_update();


-- public.sandermoduleitemvariant definition

-- Drop table

-- DROP TABLE public.sandermoduleitemvariant;

CREATE TABLE public.sandermoduleitemvariant (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	itemno varchar(20) NULL,
	code varchar(10) NULL,
	description varchar(50) NULL,
	description2 varchar(50) NULL,
	createdat timestamp DEFAULT now() NOT NULL,
	updatedat timestamp DEFAULT now() NOT NULL,
	flagneedextractkeyword int4 DEFAULT 1 NOT NULL,
	customerapprovedpartcsv text NULL,
	CONSTRAINT sandermoduleitemvariant_pkey PRIMARY KEY (id)
);

-- Table Triggers

create trigger trg_sandermoduleitemvariant_bu_updatedat before
update
    on
    public.sandermoduleitemvariant for each row execute function trg_update_column_updatedat();
create trigger trg_sandermoduleitemvariant_bu_check_update before
update
    on
    public.sandermoduleitemvariant for each row execute function trg_sandermoduleitemvariant_check_update();


-- public.sandermodulepurchaseline definition

-- Drop table

-- DROP TABLE public.sandermodulepurchaseline;

CREATE TABLE public.sandermodulepurchaseline (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	documentdate timestamp NULL,
	"no" varchar NULL,
	buyfromvendorno varchar NULL,
	buyfromvendorname varchar NULL,
	unitcost numeric NULL,
	unitcostlcy numeric NULL,
	quantity int4 NULL,
	currencycode varchar NULL,
	description2 varchar NULL,
	createdat timestamp DEFAULT now() NOT NULL,
	updatedat timestamp DEFAULT now() NOT NULL
);

-- Table Triggers

create trigger trg_sandermodulepurchaseline_bu_updatedat before
update
    on
    public.sandermodulepurchaseline for each row execute function trg_update_column_updatedat();


-- public.tb_account definition

-- Drop table

-- DROP TABLE public.tb_account;

CREATE TABLE public.tb_account (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	memberaccount varchar(100) NOT NULL,
	accountname varchar(100) NOT NULL,
	permissionid int8 NULL,
	lastlogintime timestamp NULL,
	memberpwd varchar(100) NOT NULL,
	lastmemberpwdtime timestamp NULL,
	accountemail varchar(100) NULL,
	resetpwdcode varchar(100) NULL,
	lastforgetpwdtime timestamp NULL,
	logins int4 NOT NULL,
	locktime timestamp NULL,
	logouttime timestamp NULL,
	status int4 NULL,
	lineuserid varchar(100) NULL,
	"type" varchar(100) NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	accountstatus bpchar(1) NOT NULL,
	CONSTRAINT pk_tb_account PRIMARY KEY (id)
);


-- public.tb_accountpwdlog definition

-- Drop table

-- DROP TABLE public.tb_accountpwdlog;

CREATE TABLE public.tb_accountpwdlog (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	accountid uuid NOT NULL,
	memberpwd varchar(100) NOT NULL,
	createtime timestamp NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_accountpwdlog PRIMARY KEY (id)
);


-- public.tb_accountsysrole definition

-- Drop table

-- DROP TABLE public.tb_accountsysrole;

CREATE TABLE public.tb_accountsysrole (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	accountid uuid NOT NULL,
	roleid uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_accountsysrole PRIMARY KEY (id)
);


-- public.tb_bomfiledecisionlog definition

-- Drop table

-- DROP TABLE public.tb_bomfiledecisionlog;

CREATE TABLE public.tb_bomfiledecisionlog (
	id uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	bomfilecontentid uuid NOT NULL,
	stage int4 NULL,
	step int4 NULL,
	message text NULL,
	CONSTRAINT tb_bomfiledecisionlog_pk PRIMARY KEY (id)
);


-- public.tb_bomfilequotation definition

-- Drop table

-- DROP TABLE public.tb_bomfilequotation;

CREATE TABLE public.tb_bomfilequotation (
	id uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	"no" varchar(100) NULL,
	internalpurchaseorderdate timestamp NULL,
	internalunitpriceoriginalcurrency numeric(18, 6) NULL,
	internalunitpricetwd numeric(18, 6) NULL,
	internalquantity int4 NULL,
	internalcurrency varchar(50) NULL,
	internalsuppliername varchar(100) NULL,
	externalquotationdate timestamp NULL,
	externalunitpriceoriginalcurrency numeric(18, 6) NULL,
	externalunitpricetwd numeric(18, 6) NULL,
	externalmoq int4 NULL,
	externalcurrency varchar(50) NULL,
	externalsuppliername varchar(100) NULL,
	bomfilecontentid uuid NOT NULL,
	isrecommendedno bool DEFAULT false NOT NULL,
	internallowminprice numeric(18, 6) NULL,
	internallowmaxprice numeric(18, 6) NULL,
	internalhighminprice numeric(18, 6) NULL,
	internalhighmaxprice numeric(18, 6) NULL,
	isfilterbycustomerapprovedpart bool DEFAULT false NOT NULL,
	customerapprovedpartcsv text NULL,
	matchcategory int4 NULL,
	matchfield varchar(100) NULL,
	internalsuppliercode varchar(50) NULL,
	internalitemdescription2 varchar(255) NULL,
	externalstock int4 NULL,
	externalscenario int4 NULL,
	internalquotationdate timestamp NULL,
	CONSTRAINT tb_bomfilequotation_pk PRIMARY KEY (id)
);


-- public.tb_bomfilequotationexternalhistory definition

-- Drop table

-- DROP TABLE public.tb_bomfilequotationexternalhistory;

CREATE TABLE public.tb_bomfilequotationexternalhistory (
	id uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	quotationdate timestamp NULL,
	unitpriceoriginalcurrency numeric(18, 6) NULL,
	unitpricetwd numeric(18, 6) NULL,
	moq int4 NULL,
	currency varchar(50) NULL,
	suppliername varchar(100) NULL,
	stock int4 NULL,
	manufacturerpartnumber text NULL,
	CONSTRAINT tb_bomfilequotationexternalhistory_pk PRIMARY KEY (id)
);


-- public.tb_bomfilequotationother definition

-- Drop table

-- DROP TABLE public.tb_bomfilequotationother;

CREATE TABLE public.tb_bomfilequotationother (
	id uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	quotationdate timestamp NULL,
	unitpriceoriginalcurrency numeric(18, 6) NULL,
	unitpricetwd numeric(18, 6) NULL,
	moq int4 NULL,
	currency varchar(50) NULL,
	suppliername varchar(100) NULL,
	bomfilecontentid uuid NOT NULL,
	sourcetype int4 NOT NULL,
	CONSTRAINT tb_bomfilequotationother_pk PRIMARY KEY (id)
);


-- public.tb_controllog definition

-- Drop table

-- DROP TABLE public.tb_controllog;

CREATE TABLE public.tb_controllog (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	logtime timestamp NOT NULL,
	ip varchar(100) NOT NULL,
	account varchar(100) NULL,
	"name" varchar(100) NULL,
	"exception" text NULL,
	status int4 NULL,
	controllername varchar(100) NOT NULL,
	actionname varchar(100) NOT NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	dataid uuid NULL,
	"action" int4 NULL,
	CONSTRAINT pk_tb_controllog PRIMARY KEY (id)
);


-- public.tb_file definition

-- Drop table

-- DROP TABLE public.tb_file;

CREATE TABLE public.tb_file (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	filename varchar(100) NOT NULL,
	fileformat varchar(100) NOT NULL,
	filesize int8 NULL,
	filecontent bytea NULL,
	creator uuid NOT NULL,
	createtime timestamp NOT NULL,
	updator uuid NOT NULL,
	updatetime timestamp NOT NULL,
	"type" int4 NOT NULL,
	filepath varchar(500) NULL,
	physicalfilepath varchar(500) NULL,
	filetitle varchar(100) NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_file PRIMARY KEY (id)
);


-- public.tb_sandermoduleitemkeyword definition

-- Drop table

-- DROP TABLE public.tb_sandermoduleitemkeyword;

CREATE TABLE public.tb_sandermoduleitemkeyword (
	id uuid NOT NULL,
	"no" varchar(100) NULL,
	columnname varchar(100) NULL,
	keyword text NULL,
	keywordembedding public.vector NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT tb_sander_module_item_keyword_pk PRIMARY KEY (id)
);


-- public.tb_sysfunc definition

-- Drop table

-- DROP TABLE public.tb_sysfunc;

CREATE TABLE public.tb_sysfunc (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	"name" varchar(100) NOT NULL,
	funcclassid uuid NOT NULL,
	url varchar(100) NOT NULL,
	"sequence" varchar(100) NOT NULL,
	status int4 NULL,
	memo varchar(1000) NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysfunc PRIMARY KEY (id)
);


-- public.tb_sysfuncclass definition

-- Drop table

-- DROP TABLE public.tb_sysfuncclass;

CREATE TABLE public.tb_sysfuncclass (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	classname varchar(100) NOT NULL,
	status int4 NULL,
	memo varchar(1000) NULL,
	"sequence" int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysfuncclass PRIMARY KEY (id)
);


-- public.tb_sysfuncdetail definition

-- Drop table

-- DROP TABLE public.tb_sysfuncdetail;

CREATE TABLE public.tb_sysfuncdetail (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	"name" varchar(100) NULL,
	funcid uuid NOT NULL,
	"sequence" varchar(100) NOT NULL,
	permissioncode varchar(100) NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysfuncdetail PRIMARY KEY (id)
);


-- public.tb_sysparams definition

-- Drop table

-- DROP TABLE public.tb_sysparams;

CREATE TABLE public.tb_sysparams (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	status int4 NULL,
	creator uuid NOT NULL,
	createtime timestamp NOT NULL,
	updater uuid NOT NULL,
	updatetime timestamp NOT NULL,
	"type" int4 NOT NULL,
	value varchar(100) NULL,
	"text" varchar(100) NULL,
	"group" varchar(100) NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysparams PRIMARY KEY (id)
);


-- public.tb_sysrole definition

-- Drop table

-- DROP TABLE public.tb_sysrole;

CREATE TABLE public.tb_sysrole (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	rolename varchar(100) NOT NULL,
	status int4 NULL,
	memo varchar(1000) NULL,
	linesetting int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysrole PRIMARY KEY (id)
);


-- public.tb_sysrolefuncdetail definition

-- Drop table

-- DROP TABLE public.tb_sysrolefuncdetail;

CREATE TABLE public.tb_sysrolefuncdetail (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	roleid uuid NOT NULL,
	funcdetailid uuid NOT NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_sysrolefuncdetail PRIMARY KEY (id)
);


-- public.tb_syssetting definition

-- Drop table

-- DROP TABLE public.tb_syssetting;

CREATE TABLE public.tb_syssetting (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	param varchar(100) NOT NULL,
	value varchar(100) NOT NULL,
	status int4 NULL,
	"type" varchar(100) NOT NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp NULL,
	updatedat timestamp NULL,
	CONSTRAINT pk_tb_syssetting PRIMARY KEY (id)
);


-- public.esschedulecycledbtransfer definition

-- Drop table

-- DROP TABLE public.esschedulecycledbtransfer;

CREATE TABLE public.esschedulecycledbtransfer (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	transfercode varchar(50) NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecycledbtransfer PRIMARY KEY (id),
	CONSTRAINT esschedulecycledbtransfer_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.esschedulecyclefiletransfer definition

-- Drop table

-- DROP TABLE public.esschedulecyclefiletransfer;

CREATE TABLE public.esschedulecyclefiletransfer (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	transfercode varchar(50) NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecycleexcel PRIMARY KEY (id),
	CONSTRAINT esschedulecyclefiletransfer_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.esschedulecyclelog definition

-- Drop table

-- DROP TABLE public.esschedulecyclelog;

CREATE TABLE public.esschedulecyclelog (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	runat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	durationms int4 NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	triggertype varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecyclelog PRIMARY KEY (id),
	CONSTRAINT esschedulecyclelog_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.esschedulecyclelogdetail definition

-- Drop table

-- DROP TABLE public.esschedulecyclelogdetail;

CREATE TABLE public.esschedulecyclelogdetail (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclelogid uuid NOT NULL,
	datacount int4 NOT NULL,
	errorcount int4 DEFAULT 0 NOT NULL,
	dbtransfercode varchar(100) NULL,
	filetransfercode varchar(100) NULL,
	runat timestamp NULL,
	durationms int4 NULL,
	errormessage varchar(500) NULL,
	jobstatus varchar(100) NULL,
	dbtransfercsvcode varchar(100) NULL,
	status int4 NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	transfercode varchar NULL,
	otheractiontype int4 NULL,
	CONSTRAINT esschedulecyclelogdetail_pk PRIMARY KEY (id),
	CONSTRAINT esschedulecyclelogdetail_esschedulecyclelog_fk FOREIGN KEY (schedulecyclelogid) REFERENCES public.esschedulecyclelog(id) ON DELETE CASCADE
);


-- public.esschedulecyclemonthday definition

-- Drop table

-- DROP TABLE public.esschedulecyclemonthday;

CREATE TABLE public.esschedulecyclemonthday (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	monthday int2 NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecyclemonthday PRIMARY KEY (id),
	CONSTRAINT esschedulecyclemonthday_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.esschedulecycleothertransfer definition

-- Drop table

-- DROP TABLE public.esschedulecycleothertransfer;

CREATE TABLE public.esschedulecycleothertransfer (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	actiontype int4 NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecycleothertransfer PRIMARY KEY (id),
	CONSTRAINT esschedulecycleothertransfer_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.esschedulecycleweekday definition

-- Drop table

-- DROP TABLE public.esschedulecycleweekday;

CREATE TABLE public.esschedulecycleweekday (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	schedulecyclecode varchar(50) NOT NULL,
	weekday int2 NOT NULL,
	"type" varchar(100) NULL,
	sortno varchar(5) DEFAULT 'C0000'::character varying NULL,
	priority bpchar(1) DEFAULT 'C'::bpchar NULL,
	createdat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	updatedat timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	createdby varchar(100) NULL,
	updatedby varchar(100) NULL,
	status int4 NULL,
	CONSTRAINT pk_esschedulecycleweekday PRIMARY KEY (id),
	CONSTRAINT esschedulecycleweekday_esschedulecycle_fk FOREIGN KEY (schedulecyclecode) REFERENCES public.esschedulecycle(schedulecyclecode) ON DELETE CASCADE
);


-- public.estransfererrorlog definition

-- Drop table

-- DROP TABLE public.estransfererrorlog;

CREATE TABLE public.estransfererrorlog (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
	"exception" text NULL,
	"sql" text NULL,
	schedulecyclelogdetailid uuid NOT NULL,
	CONSTRAINT estransfererrorlog_pk PRIMARY KEY (id),
	CONSTRAINT estransfererrorlog_esschedulecyclelogdetail_fk FOREIGN KEY (schedulecyclelogdetailid) REFERENCES public.esschedulecyclelogdetail(id) ON DELETE CASCADE
);
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
-- 功能管理（SysFunc）權限 990011~990014
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-5f11-7000-8000-000000990011'::uuid, 'N', 'fd2c0fde-58bb-43e7-b834-f797be46d8df'::uuid, '1', '990011', 1, NULL, NULL, '2026-05-27 14:01:21.711', '2026-05-27 14:01:21.711');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-5f12-7000-8000-000000990012'::uuid, 'N', 'fd2c0fde-58bb-43e7-b834-f797be46d8df'::uuid, '2', '990012', 1, NULL, NULL, '2026-05-27 14:01:21.711', '2026-05-27 14:01:21.711');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-5f13-7000-8000-000000990013'::uuid, 'N', 'fd2c0fde-58bb-43e7-b834-f797be46d8df'::uuid, '3', '990013', 1, NULL, NULL, '2026-05-27 14:01:21.711', '2026-05-27 14:01:21.711');
INSERT INTO public.tb_sysfuncdetail
(id, "name", funcid, "sequence", permissioncode, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-5f14-7000-8000-000000990014'::uuid, 'N', 'fd2c0fde-58bb-43e7-b834-f797be46d8df'::uuid, '4', '990014', 1, NULL, NULL, '2026-05-27 14:01:21.711', '2026-05-27 14:01:21.711');

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
-- 功能管理權限綁定系統管理員
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-6a11-7000-8000-000000990011'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019f0a10-5f11-7000-8000-000000990011'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-6a12-7000-8000-000000990012'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019f0a10-5f12-7000-8000-000000990012'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-6a13-7000-8000-000000990013'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019f0a10-5f13-7000-8000-000000990013'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');
INSERT INTO public.tb_sysrolefuncdetail
(id, roleid, funcdetailid, status, createdby, updatedby, createdat, updatedat)
VALUES('019f0a10-6a14-7000-8000-000000990014'::uuid, '5d28d5da-55de-4ccf-9d17-9e417f8c0c25'::uuid, '019f0a10-5f14-7000-8000-000000990014'::uuid, NULL, NULL, NULL, '2026-05-27 14:00:36.523', '2026-05-27 14:00:36.523');

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
