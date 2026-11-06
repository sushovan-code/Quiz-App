using BidQuiz.Application.Snapshots;

namespace BidQuiz.Application.Auth;

public sealed record AdminSessionResponse(string Token, AdminSnapshotResponse Snapshot);
