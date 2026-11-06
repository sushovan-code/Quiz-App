using BidQuiz.Application.Snapshots;

namespace BidQuiz.Application.Auth;

public sealed record TeamSessionResponse(string Token, TeamSnapshotResponse Snapshot);
