using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class TradingWorkbenchPromptTemplateTests
{
    [Fact]
    public void GetSystemPrompt_AllFifteenRolesReturnNonEmpty()
    {
        foreach (var roleId in StockAgentRoleIds.All)
        {
            var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(roleId);
            Assert.False(string.IsNullOrWhiteSpace(prompt), $"Prompt for {roleId} should not be empty");
        }
    }

    [Fact]
    public void GetSystemPrompt_AnalystRolesContainSystemShell()
    {
        var analystRoles = new[]
        {
            StockAgentRoleIds.CompanyOverviewAnalyst,
            StockAgentRoleIds.MarketAnalyst,
            StockAgentRoleIds.SocialSentimentAnalyst,
            StockAgentRoleIds.NewsAnalyst,
            StockAgentRoleIds.FundamentalsAnalyst,
            StockAgentRoleIds.ShareholderAnalyst,
            StockAgentRoleIds.ProductAnalyst
        };

        foreach (var roleId in analystRoles)
        {
            var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(roleId);
            Assert.Contains("优先调用本地 MCP 工具", prompt);
        }
    }

    [Fact]
    public void GetSystemPrompt_BackOfficeRolesContainBackOfficePrefix()
    {
        var backOfficeRoles = new[]
        {
            StockAgentRoleIds.BullResearcher,
            StockAgentRoleIds.BearResearcher,
            StockAgentRoleIds.ResearchManager,
            StockAgentRoleIds.Trader,
            StockAgentRoleIds.AggressiveRiskAnalyst,
            StockAgentRoleIds.NeutralRiskAnalyst,
            StockAgentRoleIds.ConservativeRiskAnalyst,
            StockAgentRoleIds.PortfolioManager
        };

        foreach (var roleId in backOfficeRoles)
        {
            var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(roleId);
            Assert.Contains("没有直接查询数据的权限", prompt);
        }
    }

    [Fact]
    public void GetSystemPrompt_AllPromptsEndWithChineseEnforcement()
    {
        foreach (var roleId in StockAgentRoleIds.All)
        {
            var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(roleId);
            Assert.Contains("语言与格式强制规则", prompt);
            Assert.Contains("中文输出全部分析", prompt);
        }
    }

    [Fact]
    public void GetSystemPrompt_TraderContainsFinalProposalMarker()
    {
        var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(StockAgentRoleIds.Trader);
        Assert.Contains("FINAL TRANSACTION PROPOSAL", prompt);
    }

    [Fact]
    public void GetSystemPrompt_PortfolioManagerContainsRatingOptions()
    {
        var prompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(StockAgentRoleIds.PortfolioManager);
        Assert.Contains("Buy", prompt);
        Assert.Contains("Overweight", prompt);
        Assert.Contains("Hold", prompt);
        Assert.Contains("Underweight", prompt);
        Assert.Contains("Sell", prompt);
    }

    [Fact]
    public void IsAnalystRole_ReturnsCorrectly()
    {
        Assert.True(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.CompanyOverviewAnalyst));
        Assert.True(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.MarketAnalyst));
        Assert.True(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.NewsAnalyst));
        Assert.False(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.BullResearcher));
        Assert.False(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.Trader));
        Assert.False(TradingWorkbenchPromptTemplates.IsAnalystRole(StockAgentRoleIds.PortfolioManager));
    }

    [Fact]
    public void GetSystemPrompt_UnknownRoleThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            TradingWorkbenchPromptTemplates.GetSystemPrompt("unknown_role"));
    }

    [Fact]
    public void TaskPrompts_CoverAllFifteenRoles()
    {
        Assert.Equal(15, StockAgentRoleIds.All.Count);
        foreach (var roleId in StockAgentRoleIds.All)
        {
            // Should not throw
            TradingWorkbenchPromptTemplates.GetSystemPrompt(roleId);
        }
    }
}
