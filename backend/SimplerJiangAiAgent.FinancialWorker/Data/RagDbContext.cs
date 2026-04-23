using Microsoft.Data.Sqlite;
using SimplerJiangAiAgent.FinancialWorker.Models;

namespace SimplerJiangAiAgent.FinancialWorker.Data;

public class RagDbContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public string ConnectionString => _connectionString;

    public RagDbContext(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    private void EnsureDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS chunks (
                chunk_id TEXT PRIMARY KEY,
                source_type TEXT NOT NULL DEFAULT 'financial_report',
                source_id TEXT NOT NULL,
                symbol TEXT NOT NULL,
                report_date TEXT NOT NULL,
                report_type TEXT,
                section TEXT,
                block_kind TEXT NOT NULL DEFAULT 'prose',
                page_start INTEGER,
                page_end INTEGER,
                text TEXT NOT NULL,
                tokenized_text TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE INDEX IF NOT EXISTS idx_chunks_symbol ON chunks(symbol);
            CREATE INDEX IF NOT EXISTS idx_chunks_source ON chunks(source_id);
            CREATE INDEX IF NOT EXISTS idx_chunks_report_date ON chunks(report_date);
        ";
        cmd.ExecuteNonQuery();

        // FTS5 virtual table for full-text search
        using var ftsCmd = conn.CreateCommand();
        ftsCmd.CommandText = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                tokenized_text,
                content='chunks',
                content_rowid='rowid'
            );
        ";
        ftsCmd.ExecuteNonQuery();

        // Triggers to keep FTS5 in sync
        var triggers = new[]
        {
            @"CREATE TRIGGER IF NOT EXISTS chunks_ai AFTER INSERT ON chunks BEGIN
                INSERT INTO chunks_fts(rowid, tokenized_text) VALUES (new.rowid, new.tokenized_text);
            END;",
            @"CREATE TRIGGER IF NOT EXISTS chunks_ad AFTER DELETE ON chunks BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, tokenized_text) VALUES('delete', old.rowid, old.tokenized_text);
            END;",
            @"CREATE TRIGGER IF NOT EXISTS chunks_au AFTER UPDATE ON chunks BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, tokenized_text) VALUES('delete', old.rowid, old.tokenized_text);
                INSERT INTO chunks_fts(rowid, tokenized_text) VALUES (new.rowid, new.tokenized_text);
            END;"
        };
        foreach (var trigger in triggers)
        {
            using var tCmd = conn.CreateCommand();
            tCmd.CommandText = trigger;
            tCmd.ExecuteNonQuery();
        }
    }

    /// <summary>Insert a chunk and let triggers sync FTS5.</summary>
    public void InsertChunk(FinancialChunk chunk)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO chunks 
            (chunk_id, source_type, source_id, symbol, report_date, report_type, section, block_kind, page_start, page_end, text, tokenized_text, created_at)
            VALUES 
            ($chunk_id, $source_type, $source_id, $symbol, $report_date, $report_type, $section, $block_kind, $page_start, $page_end, $text, $tokenized_text, $created_at)";
        cmd.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
        cmd.Parameters.AddWithValue("$source_type", chunk.SourceType);
        cmd.Parameters.AddWithValue("$source_id", chunk.SourceId);
        cmd.Parameters.AddWithValue("$symbol", chunk.Symbol);
        cmd.Parameters.AddWithValue("$report_date", chunk.ReportDate);
        cmd.Parameters.AddWithValue("$report_type", (object?)chunk.ReportType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$section", (object?)chunk.Section ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$block_kind", chunk.BlockKind);
        cmd.Parameters.AddWithValue("$page_start", (object?)chunk.PageStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$page_end", (object?)chunk.PageEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$text", chunk.Text);
        cmd.Parameters.AddWithValue("$tokenized_text", chunk.TokenizedText);
        cmd.Parameters.AddWithValue("$created_at", chunk.CreatedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Bulk insert chunks in a transaction.</summary>
    public void InsertChunks(IEnumerable<FinancialChunk> chunks)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var chunk in chunks)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT OR REPLACE INTO chunks 
                (chunk_id, source_type, source_id, symbol, report_date, report_type, section, block_kind, page_start, page_end, text, tokenized_text, created_at)
                VALUES 
                ($chunk_id, $source_type, $source_id, $symbol, $report_date, $report_type, $section, $block_kind, $page_start, $page_end, $text, $tokenized_text, $created_at)";
            cmd.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            cmd.Parameters.AddWithValue("$source_type", chunk.SourceType);
            cmd.Parameters.AddWithValue("$source_id", chunk.SourceId);
            cmd.Parameters.AddWithValue("$symbol", chunk.Symbol);
            cmd.Parameters.AddWithValue("$report_date", chunk.ReportDate);
            cmd.Parameters.AddWithValue("$report_type", (object?)chunk.ReportType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$section", (object?)chunk.Section ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$block_kind", chunk.BlockKind);
            cmd.Parameters.AddWithValue("$page_start", (object?)chunk.PageStart ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$page_end", (object?)chunk.PageEnd ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$text", chunk.Text);
            cmd.Parameters.AddWithValue("$tokenized_text", chunk.TokenizedText);
            cmd.Parameters.AddWithValue("$created_at", chunk.CreatedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>Delete all chunks for a given source document.</summary>
    public int DeleteChunksBySourceId(string sourceId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM chunks WHERE source_id = $source_id";
        cmd.Parameters.AddWithValue("$source_id", sourceId);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Count chunks, optionally filtered by source_id.</summary>
    public int CountChunks(string? sourceId = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        if (sourceId != null)
        {
            cmd.CommandText = "SELECT COUNT(*) FROM chunks WHERE source_id = $source_id";
            cmd.Parameters.AddWithValue("$source_id", sourceId);
        }
        else
        {
            cmd.CommandText = "SELECT COUNT(*) FROM chunks";
        }
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
