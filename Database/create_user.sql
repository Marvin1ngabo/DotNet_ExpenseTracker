-- Run this in Oracle SQL Developer, connected as SYSTEM to the pluggable database XEPDB1
-- (host localhost, port 1521, service name XEPDB1). Run as script (F5).

CREATE USER ETMS IDENTIFIED BY etms123
  DEFAULT TABLESPACE USERS
  QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW TO ETMS;

-- The app creates its tables (USERS, CATEGORIES, TRANSACTIONS, BUDGETS) in this schema on first run.
-- To browse the data afterwards, create a new SQL Developer connection as ETMS / etms123 (XEPDB1).
