using SimplerJiangAiAgent.FinancialWorker.Data;
using SimplerJiangAiAgent.FinancialWorker.Models;
using SimplerJiangAiAgent.FinancialWorker.Services.Rag;

namespace SimplerJiangAiAgent.FinancialWorker.Tests;

public class RagStorageTests : IDisposable
{
    private readonly string _dbPath;
    private readonly RagDbContext _ctx;

    public RagStorageTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"rag-test-{Guid.NewGuid():N}.db");
        _ctx = new RagDbContext($"Data Source={_dbPath}");
    }

    [Fact]
    public void DatabaseCreated_WithTablesAndFts()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
        var tables = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) tables.Add(reader.GetString(0));

        Assert.Contains("chunks", tables);
        Assert.Contains("chunks_fts", tables);
    }

    [Fact]
    public void InsertAndCount_Works()
    {
        var chunk = new FinancialChunk
        {
            SourceId = "pdf-001",
            Symbol = "600519",
            ReportDate = "2024-12-31",
            ReportType = "annual",
            Section = "营业收入",
            Text = "贵州茅台2024年营业收入1505亿元",
            TokenizedText = "贵州 茅台 2024 年 营业 收入 1505 亿元"
        };
        _ctx.InsertChunk(chunk);
        Assert.Equal(1, _ctx.CountChunks());
        Assert.Equal(1, _ctx.CountChunks("pdf-001"));
        Assert.Equal(0, _ctx.CountChunks("pdf-999"));
    }

    [Fact]
    public void BulkInsertAndDelete_Works()
    {
        var chunks = Enumerable.Range(1, 5).Select(i => new FinancialChunk
        {
            SourceId = "pdf-002",
            Symbol = "000858",
            ReportDate = "2024-06-30",
            Text = $"五粮液半年报段落{i}",
            TokenizedText = $"五粮液 半年报 段落 {i}"
        }).ToList();
        _ctx.InsertChunks(chunks);
        Assert.Equal(5, _ctx.CountChunks("pdf-002"));

        var deleted = _ctx.DeleteChunksBySourceId("pdf-002");
        Assert.Equal(5, deleted);
        Assert.Equal(0, _ctx.CountChunks("pdf-002"));
    }

    [Fact]
    public void Fts5Search_FindsChineseTokens()
    {
        _ctx.InsertChunk(new FinancialChunk
        {
            SourceId = "pdf-003",
            Symbol = "600519",
            ReportDate = "2024-12-31",
            Text = "贵州茅台2024年营业收入1505亿元，同比增长15.7%",
            TokenizedText = "贵州 茅台 2024 年 营业 收入 1505 亿元 同比 增长 15.7%"
        });
        _ctx.InsertChunk(new FinancialChunk
        {
            SourceId = "pdf-003",
            Symbol = "600519",
            ReportDate = "2024-12-31",
            Text = "贵州茅台2024年净利润为862亿元",
            TokenizedText = "贵州 茅台 2024 年 净利润 为 862 亿元"
        });

        // Search for 营业收入
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT c.text, bm25(chunks_fts) as score 
            FROM chunks_fts f 
            JOIN chunks c ON c.rowid = f.rowid 
            WHERE chunks_fts MATCH '营业 收入'
            ORDER BY bm25(chunks_fts)";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read(), "FTS5 should find a match for '营业 收入'");
        Assert.Contains("营业收入", reader.GetString(0));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }
}

public class JiebaTokenizerTests
{
    [Fact]
    public void Tokenize_ChineseText_SplitsCorrectly()
    {
        var tokenizer = new JiebaTokenizer();
        var result = tokenizer.Tokenize("贵州茅台2024年营业收入1505亿元");
        Assert.NotEmpty(result);
        Assert.Contains("茅台", result);
        Assert.Contains("收入", result);
        // Should be space-separated
        Assert.Contains(' ', result);
    }

    [Fact]
    public void Tokenize_EmptyOrNull_ReturnsEmpty()
    {
        var tokenizer = new JiebaTokenizer();
        Assert.Equal(string.Empty, tokenizer.Tokenize(""));
        Assert.Equal(string.Empty, tokenizer.Tokenize(null!));
        Assert.Equal(string.Empty, tokenizer.Tokenize("   "));
    }
}

public class NoOpEmbedderTests
{
    [Fact]
    public async Task EmbedAsync_ReturnsNull()
    {
        var embedder = new NoOpEmbedder();
        var result = await embedder.EmbedAsync("test text");
        Assert.Null(result);
        Assert.False(embedder.IsAvailable);
    }
}

public class Fts5RetrieverTests : IDisposable
{
    private readonly string _dbPath;
    private readonly RagDbContext _ctx;
    private readonly Fts5Retriever _retriever;

    public Fts5RetrieverTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"rag-retriever-test-{Guid.NewGuid():N}.db");
        _ctx = new RagDbContext($"Data Source={_dbPath}");
        _retriever = new Fts5Retriever(_ctx, new JiebaTokenizer());
    }

    private void SeedTestData()
    {
        var tokenizer = new JiebaTokenizer();
        var chunks = new List<FinancialChunk>
        {
            new()
            {
                SourceId = "doc-001",
                Symbol = "600519",
                ReportDate = "2024-12-31",
                ReportType = "Annual",
                Section = "营业收入",
                Text = "贵州茅台2024年实现营业收入1505.02亿元，同比增长15.68%。其中茅台酒收入1375亿元。",
                TokenizedText = tokenizer.Tokenize("贵州茅台2024年实现营业收入1505.02亿元，同比增长15.68%。其中茅台酒收入1375亿元。")
            },
            new()
            {
                SourceId = "doc-001",
                Symbol = "600519",
                ReportDate = "2024-12-31",
                ReportType = "Annual",
                Section = "净利润",
                Text = "公司2024年实现归属于上市公司股东的净利润862.25亿元，同比增长16.24%。",
                TokenizedText = tokenizer.Tokenize("公司2024年实现归属于上市公司股东的净利润862.25亿元，同比增长16.24%。")
            },
            new()
            {
                SourceId = "doc-002",
                Symbol = "000858",
                ReportDate = "2024-12-31",
                ReportType = "Annual",
                Section = "营业收入",
                Text = "五粮液2024年实现营业收入832.67亿元，同比增长12.3%。",
                TokenizedText = tokenizer.Tokenize("五粮液2024年实现营业收入832.67亿元，同比增长12.3%。")
            }
        };
        _ctx.InsertChunks(chunks);
    }

    [Fact]
    public async Task Retrieve_BasicQuery_FindsResults()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("营业收入");
        Assert.NotEmpty(results);
        Assert.True(results.Count >= 2); // Both 600519 and 000858 have 营业收入
    }

    [Fact]
    public async Task Retrieve_WithSymbolFilter_FiltersCorrectly()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("营业收入", symbol: "600519");
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal("600519", r.Symbol));
    }

    [Fact]
    public async Task Retrieve_WithReportTypeFilter_FiltersCorrectly()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("净利润", reportType: "Annual");
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Section == "净利润");
    }

    [Fact]
    public async Task Retrieve_EmptyQuery_ReturnsEmpty()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("");
        Assert.Empty(results);
    }

    [Fact]
    public async Task Retrieve_NoMatch_ReturnsEmpty()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("区块链加密货币");
        Assert.Empty(results);
    }

    [Fact]
    public async Task Retrieve_TopK_LimitsResults()
    {
        SeedTestData();
        var results = await _retriever.RetrieveAsync("收入", topK: 1);
        Assert.Single(results);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }
}
