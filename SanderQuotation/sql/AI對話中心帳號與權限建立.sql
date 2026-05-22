CREATE USER ai_reader WITH PASSWORD 'aireader@123';
-- 允許使用者連線到該資料庫
GRANT CONNECT ON DATABASE sander TO ai_reader;
-- 允許使用者使用特定的 Schema（通常是 public）
GRANT USAGE ON SCHEMA public TO ai_reader;
-- 允許使用者查詢該 Schema 目前「所有已存在」的資料表與檢視表（View）
GRANT SELECT ON ALL TABLES IN SCHEMA public TO ai_reader;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO ai_reader;