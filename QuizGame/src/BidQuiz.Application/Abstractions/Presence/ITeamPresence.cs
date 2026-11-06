namespace BidQuiz.Application.Abstractions.Presence;

/// <summary>Which teams have a device connected. Several devices may share a team.</summary>
public interface ITeamPresence
{
    /// <summary>Records a device for a team. Returns true if the team has just come online.</summary>
    bool Add(string connectionId, int teamId);

    /// <summary>Forgets a device. Returns the team that has just gone offline, if any.</summary>
    int? Remove(string connectionId);

    bool IsOnline(int teamId);
}
