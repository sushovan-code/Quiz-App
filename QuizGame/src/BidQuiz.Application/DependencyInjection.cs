using BidQuiz.Application.Auth.AdminLogin;
using BidQuiz.Application.Auth.AdminLogout;
using BidQuiz.Application.Auth.JoinTeam;
using BidQuiz.Application.Auth.ResumeAdmin;
using BidQuiz.Application.Auth.ResumeTeam;
using BidQuiz.Application.Bids.JudgeBid;
using BidQuiz.Application.Bids.SubmitBid;
using BidQuiz.Application.Common;
using BidQuiz.Application.Game.CloseExpiredQuestion;
using BidQuiz.Application.Game.InitializeGame;
using BidQuiz.Application.Game.ResetGame;
using BidQuiz.Application.Game.RevealAnswer;
using BidQuiz.Application.Game.RevealBids;
using BidQuiz.Application.Game.StartQuestion;
using BidQuiz.Application.Options;
using BidQuiz.Application.Scores;
using BidQuiz.Application.Setup;
using BidQuiz.Application.Setup.AddQuestion;
using BidQuiz.Application.Setup.AddTeam;
using BidQuiz.Application.Setup.DeleteQuestion;
using BidQuiz.Application.Setup.DeleteTeam;
using BidQuiz.Application.Setup.DrawQuestions;
using BidQuiz.Application.Setup.GetSetup;
using BidQuiz.Application.Setup.ListTeamOptions;
using BidQuiz.Application.Setup.SetQuestionsPerGame;
using BidQuiz.Application.Setup.SetTeamsPerGame;
using BidQuiz.Application.Setup.UpdateQuestion;
using BidQuiz.Application.Setup.UpdateTeam;
using BidQuiz.Application.Snapshots.GetAdminSnapshot;
using BidQuiz.Application.Snapshots.GetTeamSnapshot;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Scoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BidQuiz.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases. Everything is a singleton: game state lives in the database and the
    /// in-process pieces (gate, deadline) must be shared by every request.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<GameGate>();
        services.AddSingleton<QuestionDeadline>();
        services.AddSingleton<Scoreboard>();
        services.AddSingleton<SetupChanges>();
        services.AddSingleton<GameSeeder>();
        services.AddSingleton<IQuestionPicker, RandomQuestionPicker>();
        services.AddSingleton<IScoringPolicy>(provider =>
            new HighestBidScoringPolicy(provider.GetRequiredService<IOptions<ScoringOptions>>().Value.ToRules()));

        return services.AddUseCases();
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services) => services
        .AddSingleton<JoinTeamHandler>()
        .AddSingleton<ResumeTeamHandler>()
        .AddSingleton<AdminLoginHandler>()
        .AddSingleton<ResumeAdminHandler>()
        .AddSingleton<AdminLogoutHandler>()
        .AddSingleton<GetTeamSnapshotHandler>()
        .AddSingleton<GetAdminSnapshotHandler>()
        .AddSingleton<InitializeGameHandler>()
        .AddSingleton<StartQuestionHandler>()
        .AddSingleton<CloseExpiredQuestionHandler>()
        .AddSingleton<RevealBidsHandler>()
        .AddSingleton<RevealAnswerHandler>()
        .AddSingleton<ResetGameHandler>()
        .AddSingleton<SubmitBidHandler>()
        .AddSingleton<JudgeBidHandler>()
        .AddSingleton<ListTeamOptionsHandler>()
        .AddSingleton<GetSetupHandler>()
        .AddSingleton<AddTeamHandler>()
        .AddSingleton<UpdateTeamHandler>()
        .AddSingleton<DeleteTeamHandler>()
        .AddSingleton<AddQuestionHandler>()
        .AddSingleton<UpdateQuestionHandler>()
        .AddSingleton<DeleteQuestionHandler>()
        .AddSingleton<SetQuestionsPerGameHandler>()
        .AddSingleton<SetTeamsPerGameHandler>()
        .AddSingleton<DrawQuestionsHandler>();
}
