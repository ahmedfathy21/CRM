using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Models.Enums;

namespace CRM.Features.CRM.Common.Services;

public static class DealStageTransitionService
{
    private static readonly Dictionary<DealStage, HashSet<DealStage>> AllowedTransitions = new()
    {
        { DealStage.Lead, [DealStage.Qualified, DealStage.Lost] },
        { DealStage.Qualified, [DealStage.Proposal, DealStage.Lost] },
        { DealStage.Proposal, [DealStage.Negotiation, DealStage.Lost] },
        { DealStage.Negotiation, [DealStage.Won, DealStage.Lost] },
        { DealStage.Won, [] },
        { DealStage.Lost, [DealStage.Lead] },
    };

    private static readonly Dictionary<DealStage, int> StageProbabilities = new()
    {
        { DealStage.Lead, 10 },
        { DealStage.Qualified, 25 },
        { DealStage.Proposal, 50 },
        { DealStage.Negotiation, 75 },
        { DealStage.Won, 100 },
        { DealStage.Lost, 0 },
    };

    public static Result<(DealStage NewStage, int NewProbability)> MoveToStage(
        DealStage current, DealStage next)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed))
            return Result.Failure<(DealStage, int)>(
                Error.BadRequest($"Unknown deal stage: {current}."));

        if (!allowed.Contains(next))
        {
            var allowedList = string.Join(", ", allowed.Select(s => s.ToString()));
            return Result.Failure<(DealStage, int)>(
                Error.BadRequest($"Invalid transition from {current} to {next}. " +
                    $"Allowed transitions from {current} are: {allowedList}."));
        }

        var probability = StageProbabilities.GetValueOrDefault(next, 0);
        return Result.Success((next, probability));
    }

    public static int GetProbability(DealStage stage) =>
        StageProbabilities.GetValueOrDefault(stage, 0);
}
