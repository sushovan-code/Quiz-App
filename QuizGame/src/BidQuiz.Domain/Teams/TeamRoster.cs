using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Teams;

/// <summary>Decides who plays: the first TeamsPerGame teams on the roster.</summary>
public static class TeamRoster
{
    /// <param name="state">The game, whose team count is kept within the roster size.</param>
    /// <param name="teamsInRosterOrder">Every team, in roster order (by id).</param>
    public static void Apply(GameState state, IReadOnlyList<Team> teamsInRosterOrder)
    {
        // Deleting teams can leave the count above the roster size.
        state.SetTeamsPerGame(Math.Max(1, Math.Min(state.TeamsPerGame, teamsInRosterOrder.Count)));

        for (var index = 0; index < teamsInRosterOrder.Count; index++)
        {
            teamsInRosterOrder[index].SetActive(index < state.TeamsPerGame);
        }
    }
}
