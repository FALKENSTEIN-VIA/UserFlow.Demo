-- *****************************************************************************************
-- @file ChangeStreamsTriggers.sql
-- @author Claus Falkenstein
-- @company VIA Software GmbH
-- @date 2025-05-12
-- @brief Defines PostgreSQL triggers for ChangeStreams (fully corrected per table PK names)
-- @details
-- This script ensures that all database operations (INSERT, UPDATE, DELETE)
-- generate a NOTIFY 'table_changed' event with consistent JSON payload.
-- The 'entityId' is always sent as TEXT using the correct PK column per table.
-- *****************************************************************************************

-- 💡 Users (AspNetUsers uses 'Id')
CREATE OR REPLACE FUNCTION notify_users_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'Users',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_users_changed
AFTER INSERT OR UPDATE OR DELETE ON "AspNetUsers"
FOR EACH ROW EXECUTE FUNCTION notify_users_change();

-- 💡 Projects (uses 'Id')
CREATE OR REPLACE FUNCTION notify_projects_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'Projects',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_projects_changed
AFTER INSERT OR UPDATE OR DELETE ON "Projects"
FOR EACH ROW EXECUTE FUNCTION notify_projects_change();

-- 💡 Companies (uses 'Id')
CREATE OR REPLACE FUNCTION notify_companies_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'Companies',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_companies_changed
AFTER INSERT OR UPDATE OR DELETE ON "Companies"
FOR EACH ROW EXECUTE FUNCTION notify_companies_change();

-- 💡 Notes (uses 'Id')
CREATE OR REPLACE FUNCTION notify_notes_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'Notes',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_notes_changed
AFTER INSERT OR UPDATE OR DELETE ON "Notes"
FOR EACH ROW EXECUTE FUNCTION notify_notes_change();

-- 💡 Screens (uses 'Id')
CREATE OR REPLACE FUNCTION notify_screens_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'Screens',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_screens_changed
AFTER INSERT OR UPDATE OR DELETE ON "Screens"
FOR EACH ROW EXECUTE FUNCTION notify_screens_change();

-- 💡 ScreenActions (uses 'ScreenActionId')
CREATE OR REPLACE FUNCTION notify_screenactions_change()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM pg_notify(
    'table_changed',
    json_build_object(
      'entityName', 'ScreenActions',
      'operation', TG_OP,
      'entityId', COALESCE(NEW."Id", OLD."Id")::text,
      'changedAt', now()
    )::text
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER trigger_screenactions_changed
AFTER INSERT OR UPDATE OR DELETE ON "ScreenActions"
FOR EACH ROW EXECUTE FUNCTION notify_screenactions_change();
