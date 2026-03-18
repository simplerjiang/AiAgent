using System.Text.Json;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentResultNormalizerTests
{
    [Fact]
    public void Normalize_CommanderAddsMissingStep3Fields()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "commander",
          "summary": "ok",
          "confidence_score": 70
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.Commander, input.RootElement);

        Assert.True(normalized.TryGetProperty("evidence", out _));
        Assert.True(normalized.TryGetProperty("analysis_opinion", out _));
        Assert.True(normalized.TryGetProperty("trigger_conditions", out _));
        Assert.True(normalized.TryGetProperty("invalid_conditions", out _));
        Assert.True(normalized.TryGetProperty("risk_warning", out _));

        Assert.True(normalized.TryGetProperty("revision", out var revision));
        Assert.True(revision.TryGetProperty("required", out _));
        Assert.True(revision.TryGetProperty("reason", out _));
        Assert.True(revision.TryGetProperty("previousDirection", out _));

        Assert.True(normalized.TryGetProperty("consistency", out var consistency));
        Assert.True(consistency.TryGetProperty("shortTermTrend", out _));
        Assert.True(consistency.TryGetProperty("midTermTrend", out _));
        Assert.True(consistency.TryGetProperty("status", out _));

        Assert.True(normalized.TryGetProperty("marketState", out var marketState));
        Assert.True(marketState.TryGetProperty("state", out _));
        Assert.True(marketState.TryGetProperty("hysteresisApplied", out _));
        Assert.True(marketState.TryGetProperty("strongCounterEvidence", out _));
    }

    [Fact]
    public void Normalize_StockNewsKeepsAgentAndAddsDefaults()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "stock_news",
          "summary": "news"
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.StockNews, input.RootElement);

        Assert.Equal("stock_news", normalized.GetProperty("agent").GetString());
        Assert.True(normalized.TryGetProperty("confidence", out _));
        Assert.True(normalized.TryGetProperty("events", out var events));
        Assert.Equal(JsonValueKind.Array, events.ValueKind);
        Assert.True(normalized.TryGetProperty("evidence", out var evidence));
        Assert.Equal(JsonValueKind.Array, evidence.ValueKind);

        var populated = StockAgentResultNormalizer.Normalize(StockAgentKind.StockNews, JsonDocument.Parse("""
        {
          "agent": "stock_news",
          "summary": "news",
          "evidence": [
            {
              "point": "消息",
              "source": "新浪",
              "publishedAt": "2026-03-17 09:00"
            }
          ]
        }
        """).RootElement);

        var evidenceItem = populated.GetProperty("evidence")[0];
        Assert.True(evidenceItem.TryGetProperty("title", out _));
        Assert.True(evidenceItem.TryGetProperty("excerpt", out _));
        Assert.True(evidenceItem.TryGetProperty("readMode", out _));
        Assert.True(evidenceItem.TryGetProperty("readStatus", out _));
        Assert.True(evidenceItem.TryGetProperty("localFactId", out _));
    }

    [Fact]
    public void Normalize_StockNewsWithoutUsableEvidence_DowngradesToNeutralWatch()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "stock_news",
          "summary": "看多",
          "confidence": 88,
          "evidence": [
            {
              "point": "消息",
              "source": "",
              "publishedAt": null
            }
          ]
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.StockNews, input.RootElement);

        Assert.Equal("信息不足：缺少可验证来源或发布时间，建议观望。", normalized.GetProperty("summary").GetString());
        Assert.Equal(20, normalized.GetProperty("confidence").GetInt32());
        Assert.Contains("观望", normalized.GetProperty("signals")[0].GetString());
    }

    [Fact]
    public void Normalize_StockNewsWithMetadataOnlyEvidence_DowngradesToNeutralWatch()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "stock_news",
          "summary": "偏多",
          "confidence": 81,
          "evidence": [
            {
              "point": "标题线索",
              "source": "新浪",
              "publishedAt": "2026-03-17 09:00",
              "readStatus": "metadata_only"
            }
          ]
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.StockNews, input.RootElement);

        Assert.Equal("信息不足：缺少可验证来源或发布时间，建议观望。", normalized.GetProperty("summary").GetString());
        Assert.Equal(20, normalized.GetProperty("confidence").GetInt32());
    }

    [Fact]
    public void Normalize_SectorNewsWithoutUsableEvidence_DowngradesToNeutralWatch()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "sector_news",
          "summary": "偏多",
          "confidence": 75,
          "evidence": [
            {
              "point": "板块热度",
              "source": "未知",
              "publishedAt": "not-a-time"
            }
          ]
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.SectorNews, input.RootElement);

        Assert.Equal("信息不足：缺少可验证来源或发布时间，建议观望。", normalized.GetProperty("summary").GetString());
        Assert.Equal(20, normalized.GetProperty("confidence").GetInt32());
        Assert.Contains("观望", normalized.GetProperty("signals")[0].GetString());
    }

    [Fact]
    public void Normalize_FlattensProbabilityAndScoringMetrics()
    {
        using var input = JsonDocument.Parse("""
        {
          "agent": "sector_news",
          "summary": "偏多",
          "confidence": 78,
          "probability_analysis": {
            "up_probability": 65,
            "down_probability": 35
          },
          "entryScore": 82,
          "valuationScore": 60,
          "positionPercent": 20,
          "evidence": [
            {
              "point": "板块走强",
              "source": "新浪",
              "publishedAt": "2026-03-17 10:00"
            }
          ]
        }
        """);

        var normalized = StockAgentResultNormalizer.Normalize(StockAgentKind.SectorNews, input.RootElement);
        var metrics = normalized.GetProperty("metrics");

        Assert.Equal(65m, metrics.GetProperty("riseProbability").GetDecimal());
        Assert.Equal(35m, metrics.GetProperty("fallProbability").GetDecimal());
        Assert.Equal(82m, metrics.GetProperty("entryScore").GetDecimal());
        Assert.Equal(60m, metrics.GetProperty("valuationScore").GetDecimal());
        Assert.Equal(20m, metrics.GetProperty("positionPercent").GetDecimal());
    }
}
