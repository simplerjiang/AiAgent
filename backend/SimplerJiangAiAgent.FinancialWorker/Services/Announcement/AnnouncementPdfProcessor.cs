using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SimplerJiangAiAgent.FinancialWorker.Data;
using SimplerJiangAiAgent.FinancialWorker.Models;
using SimplerJiangAiAgent.FinancialWorker.Services.Rag;
using UglyToad.PdfPig;

namespace SimplerJiangAiAgent.FinancialWorker.Services.Announcement;

/// <summary>
/// 处理已下载的公告 PDF：提取文本 → 切块 → jieba 分词 → 写入 RAG 数据库。
/// </summary>
public class AnnouncementPdfProcessor
{
    private const int WholeDocThreshold = 2000;

    private static readonly Regex AnnouncementTypeRegex = new(
        @"(分红|派息|送股|转增|配股|减持|增持|回购|质押|解质|股权激励|限售|解禁|收购|重组|定增|可转债|担保|关联交易|诉讼|仲裁|业绩预告|业绩快报|年度报告|半年度报告|季度报告)",
        RegexOptions.Compiled);

    private readonly RagDbContext _ragDb;
    private readonly IChineseTokenizer _tokenizer;
    private readonly ILogger<AnnouncementPdfProcessor> _logger;

    public AnnouncementPdfProcessor(
        RagDbContext ragDb,
        IChineseTokenizer tokenizer,
        ILogger<AnnouncementPdfProcessor> logger)
    {
        _ragDb = ragDb;
        _tokenizer = tokenizer;
        _logger = logger;
    }

    /// <summary>
    /// 处理已下载的公告 PDF，提取文本、切块、写入 RAG 数据库。
    /// </summary>
    /// <returns>成功入库的 chunk 数量</returns>
    public async Task<int> ProcessAsync(
        List<DownloadedAnnouncementPdf> pdfs,
        CancellationToken ct = default)
    {
        var totalChunks = 0;

        foreach (var pdf in pdfs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var count = await ProcessSingleAsync(pdf, ct);
                totalChunks += count;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AnnPdf] 处理失败，跳过: {ArtCode} {Title}",
                    pdf.ArtCode, pdf.Title);
            }
        }

        _logger.LogInformation("[AnnPdf] 批量处理完成: {Total} chunks from {Count} PDFs",
            totalChunks, pdfs.Count);
        return totalChunks;
    }

    private Task<int> ProcessSingleAsync(DownloadedAnnouncementPdf pdf, CancellationToken ct)
    {
        // 去重：检查该 art_code 是否已入库
        var existing = _ragDb.CountChunks(pdf.ArtCode);
        if (existing > 0)
        {
            _logger.LogDebug("[AnnPdf] 跳过已入库: {ArtCode} ({Existing} chunks)", pdf.ArtCode, existing);
            return Task.FromResult(0);
        }

        // 提取文本
        var text = ExtractText(pdf.FilePath);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("[AnnPdf] 提取文本为空: {ArtCode} {File}", pdf.ArtCode, pdf.FilePath);
            return Task.FromResult(0);
        }

        // 切块
        var paragraphs = ChunkText(text);

        // 识别公告类型
        var reportType = InferAnnouncementType(pdf.Title);

        // 构建 chunks
        var symbol = pdf.Symbol;
        var reportDate = pdf.PublishTime.ToString("yyyy-MM-dd");
        var chunks = new List<FinancialChunk>();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var para = paragraphs[i];
            if (string.IsNullOrWhiteSpace(para)) continue;

            var tokenized = _tokenizer.Tokenize(para);
            chunks.Add(new FinancialChunk
            {
                ChunkId = $"ann_{pdf.ArtCode}_{i}",
                SourceType = "announcement",
                SourceId = pdf.ArtCode,
                Symbol = symbol,
                ReportDate = reportDate,
                ReportType = reportType,
                Section = i.ToString(),
                BlockKind = "prose",
                Text = para,
                TokenizedText = tokenized,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (chunks.Count == 0)
            return Task.FromResult(0);

        // 批量写入
        _ragDb.InsertChunks(chunks);
        _logger.LogInformation("[AnnPdf] 入库: {ArtCode} \"{Title}\" → {Count} chunks",
            pdf.ArtCode, pdf.Title, chunks.Count);

        return Task.FromResult(chunks.Count);
    }

    /// <summary>使用 PdfPig 提取 PDF 全文</summary>
    internal static string ExtractText(string pdfPath)
    {
        using var document = PdfDocument.Open(pdfPath);
        var sb = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText);
                sb.AppendLine(); // 页间空行
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>按段落切块。短文档整体作为一个 chunk，长文档按双换行分割。</summary>
    internal static List<string> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        text = text.Trim();

        if (text.Length <= WholeDocThreshold)
            return new List<string> { text };

        // 按双换行分割
        var parts = Regex.Split(text, @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        // 如果分割后只剩一段（没有双换行），按单换行再分
        if (parts.Count <= 1 && text.Length > WholeDocThreshold)
        {
            parts = text.Split('\n')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        return parts;
    }

    /// <summary>从公告标题推断公告类型</summary>
    internal static string? InferAnnouncementType(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var match = AnnouncementTypeRegex.Match(title);
        return match.Success ? match.Value : null;
    }
}
