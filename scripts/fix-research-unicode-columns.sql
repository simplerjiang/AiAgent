-- Fix CJK encoding for Research tables (SQL Server only)
-- These columns were created as VARCHAR (non-Unicode) and need to be NVARCHAR for CJK support.
-- Run this against existing SQL Server databases where ResearchSessions/ResearchTurns already exist.
-- Not needed for SQLite (TEXT columns are already Unicode).

ALTER TABLE ResearchSessions ALTER COLUMN Name NVARCHAR(MAX);
ALTER TABLE ResearchSessions ALTER COLUMN LastUserIntent NVARCHAR(MAX);
ALTER TABLE ResearchSessions ALTER COLUMN LatestDecisionHeadline NVARCHAR(MAX);
ALTER TABLE ResearchSessions ALTER COLUMN ActiveStage NVARCHAR(MAX);
ALTER TABLE ResearchTurns ALTER COLUMN UserPrompt NVARCHAR(MAX);
