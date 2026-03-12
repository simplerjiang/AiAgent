using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class SourceGovernancePolicy
{
    public static (string Status, string Reason) EvaluateSourceStatus(NewsSourceRegistry source, SourceGovernanceOptions options)
    {
        if (source.ConsecutiveFailures >= options.MaxConsecutiveFailures)
        {
            return (NewsSourceStatus.Quarantine, "consecutive_failures");
        }

        if (source.ParseSuccessRate.HasValue && source.ParseSuccessRate.Value < options.MinParseSuccessRate)
        {
            return (NewsSourceStatus.Quarantine, "low_parse_success");
        }

        if (source.TimestampCoverage.HasValue && source.TimestampCoverage.Value < options.MinTimestampCoverage)
        {
            return (NewsSourceStatus.Quarantine, "low_timestamp_coverage");
        }

        if (source.FreshnessLagMinutes.HasValue && source.FreshnessLagMinutes.Value > options.MaxFreshnessLagMinutes)
        {
            return (NewsSourceStatus.Quarantine, "stale_source");
        }

        return (NewsSourceStatus.Active, "healthy");
    }

    public static bool CanPromoteCandidate(NewsSourceCandidate candidate, SourceGovernanceOptions options)
    {
        if (!candidate.VerificationScore.HasValue || candidate.VerificationScore.Value < options.CandidatePromotionScore)
        {
            return false;
        }

        if (!candidate.ParseSuccessRate.HasValue || candidate.ParseSuccessRate.Value < options.MinParseSuccessRate)
        {
            return false;
        }

        if (!candidate.TimestampCoverage.HasValue || candidate.TimestampCoverage.Value < options.MinTimestampCoverage)
        {
            return false;
        }

        if (candidate.FreshnessLagMinutes.HasValue && candidate.FreshnessLagMinutes.Value > options.MaxFreshnessLagMinutes)
        {
            return false;
        }

        return true;
    }
}