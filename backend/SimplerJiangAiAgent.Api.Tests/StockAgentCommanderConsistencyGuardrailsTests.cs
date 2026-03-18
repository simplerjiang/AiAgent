using System.Text.Json;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentCommanderConsistencyGuardrailsTests
{
    [Fact]
    public void Apply_WhenTimeframesConflict_MarksDivergence()
    {
        using var commanderData = JsonDocument.Parse("""
        {
          "agent": "commander",
          "summary": "ok",
          "analysis_opinion": "当前更适合观察，等待确认。",
          "confidence_score": 70,
          "revision": { "required": false, "reason": null, "previousDirection": null },
          "signals": [],
          "invalidations": []
        }
        """);

        var deps = new[]
        {
            new StockAgentResultDto(
                "trend_analysis",
                "走势分析Agent",
                true,
                null,
                JsonDocument.Parse("""
                {
                  "timeframeSignals": [
                    { "timeframe": "1D", "trend": "上涨" },
                    { "timeframe": "1W", "trend": "下跌" }
                  ]
                }
                """).RootElement,
                null)
        };

        var contextJson = """
        {
          "commanderHistory": { "items": [] },
          "kLines": []
        }
        """;

        var guarded = StockAgentCommanderConsistencyGuardrails.Apply(commanderData.RootElement, deps, contextJson);

        Assert.Equal("分歧态", guarded.GetProperty("consistency").GetProperty("status").GetString());
        Assert.Contains("分歧态", guarded.GetProperty("signals")[0].GetString());
    }

    [Fact]
    public void Apply_WhenDirectionChangesWithLowConfidence_UsesHysteresis()
    {
        using var commanderData = JsonDocument.Parse("""
        {
          "agent": "commander",
          "summary": "ok",
          "analysis_opinion": "倾向加仓，但确认还不充分。",
          "confidence_score": 55,
          "revision": { "required": false, "reason": null, "previousDirection": null },
          "signals": [],
          "invalid_conditions": "量能未确认"
        }
        """);

        var deps = Array.Empty<StockAgentResultDto>();
        var contextJson = """
        {
          "commanderHistory": {
            "items": [
              {
                "createdAt": "2026-03-02T09:30:00",
                "direction": "观察",
                "confidence": 68
              }
            ]
          },
          "kLines": []
        }
        """;

        var guarded = StockAgentCommanderConsistencyGuardrails.Apply(commanderData.RootElement, deps, contextJson);

        Assert.Contains("观察", guarded.GetProperty("analysis_opinion").GetString());
        Assert.True(guarded.GetProperty("marketState").GetProperty("hysteresisApplied").GetBoolean());
        Assert.True(guarded.GetProperty("revision").GetProperty("required").GetBoolean());
        Assert.Equal("观察", guarded.GetProperty("revision").GetProperty("previousDirection").GetString());
    }

    [Fact]
    public void Apply_WhenStrongCounterEvidenceExists_AllowsDirectionChange()
    {
        using var commanderData = JsonDocument.Parse("""
        {
          "agent": "commander",
          "summary": "ok",
          "analysis_opinion": "应当减仓，防止继续破位。",
          "confidence_score": 58,
          "revision": { "required": false, "reason": null, "previousDirection": null },
          "signals": [],
          "invalid_conditions": "关键位跌破；止损条件触发"
        }
        """);

        var deps = Array.Empty<StockAgentResultDto>();
        var contextJson = """
        {
          "commanderHistory": {
            "items": [
              {
                "createdAt": "2026-03-02T09:30:00",
                "direction": "加仓",
                "confidence": 70
              }
            ]
          },
          "kLines": []
        }
        """;

        var guarded = StockAgentCommanderConsistencyGuardrails.Apply(commanderData.RootElement, deps, contextJson);

        Assert.Contains("减仓", guarded.GetProperty("analysis_opinion").GetString());
        Assert.True(guarded.GetProperty("marketState").GetProperty("strongCounterEvidence").GetBoolean());
        Assert.Equal("强反证触发，允许方向变更", guarded.GetProperty("marketState").GetProperty("overrideReason").GetString());
        Assert.True(guarded.GetProperty("revision").GetProperty("required").GetBoolean());
    }

    [Fact]
    public void Apply_WhenEvidenceIsOnlyMetadata_CapsCommanderConfidence()
    {
        using var commanderData = JsonDocument.Parse("""
        {
          "agent": "commander",
          "summary": "ok",
          "analysis_opinion": "偏多，但证据仍需进一步核验。",
          "confidence_score": 88,
          "revision": { "required": false, "reason": null, "previousDirection": null },
          "signals": [],
          "invalidations": [],
          "evidence": [
            {
              "point": "消息标题",
              "source": "新浪",
              "publishedAt": "2026-03-17 09:00",
              "readStatus": "metadata_only"
            }
          ]
        }
        """);

        var guarded = StockAgentCommanderConsistencyGuardrails.Apply(
            commanderData.RootElement,
            Array.Empty<StockAgentResultDto>(),
            """
            {
              "commanderHistory": { "items": [] },
              "kLines": []
            }
            """);

        Assert.Equal(55m, guarded.GetProperty("confidence_score").GetDecimal());
        Assert.Contains("证据仅为元数据或未核验", guarded.GetProperty("signals")[0].GetString());
    }
}
