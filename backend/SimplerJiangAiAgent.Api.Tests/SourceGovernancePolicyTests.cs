using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Jobs;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class SourceGovernancePolicyTests
{
    [Fact]
    public void EvaluateSourceStatus_WhenConsecutiveFailuresExceeded_Quarantines()
    {
        var source = new NewsSourceRegistry
        {
            Domain = "example.com",
            ConsecutiveFailures = 4,
            ParseSuccessRate = 0.95m,
            TimestampCoverage = 0.95m,
            FreshnessLagMinutes = 30
        };

        var options = new SourceGovernanceOptions { MaxConsecutiveFailures = 3 };
        var result = SourceGovernancePolicy.EvaluateSourceStatus(source, options);

        Assert.Equal(NewsSourceStatus.Quarantine, result.Status);
        Assert.Equal("consecutive_failures", result.Reason);
    }

    [Fact]
    public void EvaluateSourceStatus_WhenMetricsHealthy_StaysActive()
    {
        var source = new NewsSourceRegistry
        {
            Domain = "example.com",
            ConsecutiveFailures = 0,
            ParseSuccessRate = 0.96m,
            TimestampCoverage = 0.98m,
            FreshnessLagMinutes = 15
        };

        var options = new SourceGovernanceOptions();
        var result = SourceGovernancePolicy.EvaluateSourceStatus(source, options);

        Assert.Equal(NewsSourceStatus.Active, result.Status);
        Assert.Equal("healthy", result.Reason);
    }

    [Fact]
    public void CanPromoteCandidate_WhenScoreAndMetricsPass_ReturnsTrue()
    {
        var candidate = new NewsSourceCandidate
        {
            Domain = "sample.org",
            VerificationScore = 0.88m,
            ParseSuccessRate = 0.92m,
            TimestampCoverage = 0.96m,
            FreshnessLagMinutes = 30
        };

        var options = new SourceGovernanceOptions
        {
            CandidatePromotionScore = 0.80m,
            MinParseSuccessRate = 0.85m,
            MinTimestampCoverage = 0.90m,
            MaxFreshnessLagMinutes = 120
        };

        var ok = SourceGovernancePolicy.CanPromoteCandidate(candidate, options);
        Assert.True(ok);
    }
}