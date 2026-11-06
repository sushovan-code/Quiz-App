using BidQuiz.Domain.Common;

namespace BidQuiz.Domain.Games;

public static class GameErrors
{
    public static readonly Error QuestionRunning =
        Error.Conflict("game.question_running", "A question is already running.");

    public static readonly Error TimeUp =
        Error.Conflict("game.time_up", "Time is up. Your bid was not accepted.");

    public static readonly Error NothingAsked =
        Error.Conflict("game.nothing_asked", "No question has been asked yet.");

    public static readonly Error RevealBidsFirst =
        Error.Conflict("game.reveal_bids_first", "Reveal the bids first.");

    public static readonly Error SetupLocked =
        Error.Conflict("setup.locked", "Reset the game before changing the setup.");

    public static readonly Error NotEnoughQuestions =
        Error.Validation("setup.not_enough_questions", "There aren't that many questions in the bank.");

    public static readonly Error QuestionsPerGameOutOfRange = Error.Validation(
        "setup.questions_per_game",
        $"Questions per game must be between {GameLimits.MinQuestionsPerGame} and {GameLimits.MaxQuestionsPerGame}.");

    public static readonly Error TeamsPerGameOutOfRange =
        Error.Validation("setup.teams_per_game", "Choose between 1 team and every team on the roster.");

    public static Error BankTooSmall(int questionsPerGame) => Error.Conflict(
        "setup.bank_too_small",
        $"The bank needs at least {questionsPerGame} questions for this game.");
}
