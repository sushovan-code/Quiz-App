using BidQuiz.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Api.Game;

/// <summary>Decides who plays: the first TeamsPerGame teams on the roster. The caller saves the changes.</summary>
public static class TeamRoster
{
    public static async Task ApplyAsync(GameDbContext db)
    {
        var state = await db.GameStates.SingleAsync();
        var teams = await db.Teams.OrderBy(t => t.Id).ToListAsync();

        // Deleting teams can leave the count above the roster size.
        state.TeamsPerGame = Math.Max(1, Math.Min(state.TeamsPerGame, teams.Count));

        for (var index = 0; index < teams.Count; index++)
        {
            teams[index].Active = index < state.TeamsPerGame;
        }
    }
}
