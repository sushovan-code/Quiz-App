# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Bid Quiz: a live bidding quiz. The host runs it on a projector laptop (`/host`), and teams play on phones (`/play`) over the venue LAN. Teams guess movies and bid on how sure they are. All the code lives under `QuizGame/`:

- `server/`: ASP.NET Core 10, SignalR, EF Core with SQLite. A single project, `BidQuiz.Server`.
- `client/`: Angular 22 (standalone components, signals). It builds into `server/wwwroot`.
- `tests/BidQuiz.Tests/`: xUnit.

`QuizGame/README.md` is the detailed design doc: game rules, data model, the message flow per screen, and the reliability table. Read the relevant section before changing game flow.

## Standards

`docs/standards/*.md` override the defaults (naming, architecture, coding, testing, security, UI theming, workflow). The code was brought up to them one slice per branch: the Domain/Application/Infrastructure/Api split, `Result<T>`, `CancellationToken`s, `Directory.Build.props`, TypeScript `strict`, ESLint and Aspire. Write new code to the standards.

## Commands

Run these from `QuizGame/`:

```
dotnet run --project src/BidQuiz.AppHost          # dev: API + ng serve (http://localhost:4200) + Aspire dashboard
dotnet run --project src/BidQuiz.Api              # LAN show: API serving the built client. The first run does npm ci + ng build (about a minute)
dotnet build BidQuiz.slnx -p:SkipClientBuild=true # builds the API only
dotnet test BidQuiz.slnx -p:SkipClientBuild=true  # runs all tests. Skipping the client build is fine, the tests don't need it
dotnet test BidQuiz.slnx -p:SkipClientBuild=true --filter "FullyQualifiedName~GameServiceTests.Tick_ClosesOnceAtTheDeadline"
```

Frontend dev loop: the AppHost runs `npm run start:aspire` in `src/web/bidquiz-web` on port 4200, unproxied. `proxy.conf.js` forwards `/api`, `/openapi` and `/hubs` (WebSockets) to the API address from Aspire service discovery (`services__api__http__0`), or to port 5000 under plain `npm start`. `npm run build` builds the client alone. There are no frontend tests yet. `/health` and `/alive` (ServiceDefaults) are mapped in Development only.

The API listens on `0.0.0.0:5000` unless `urls`/`http_ports`/`WEBSITE_SITE_NAME` is set (Aspire sets `urls`), and logs the LAN join URL at startup. The admin password comes from user secrets (`Game:AdminPassword`), and team passwords come from `src/BidQuiz.Api/seed/teams.json`.

## Architecture (big picture)

- **Realtime-first.** Almost everything goes over one SignalR hub, `GameHub` at `/hubs/game`. The only HTTP endpoints are `GET /api/teams` and `GET /api/config`, in `Program.cs`. Hub methods are the public contract. Clients call them by name, so renaming one is a breaking change on both sides.
- **Who is calling** is stored in `Context.Items` on the connection (`teamId` and `isAdmin`), set by `JoinTeam`/`ResumeTeam`/`AdminLogin`/`ResumeAdmin`. Every admin hub method must call `RequireAdmin()`. Team tokens are stored in the database (`Teams.SessionToken`). Admin tokens are kept in memory, so the host logs in again after a restart.
- **Groups decide who gets what.** `SignalRGameNotifier` sends to the `admin` group, the `teams` group, or both. Bids and `CorrectAnswer` must never reach the `teams` group, and `HubSecurityTests` enforces this. `QuestionDto` deliberately has no `CorrectAnswer`, while `QuestionSetupDto` does and is admin-only.
- **Services:** `IGameService` (play), `ISetupService` (teams, the question bank, counts, locked once the first question starts) and `IAuthService`. All are singletons that open a short-lived `GameDbContext` via `IDbContextFactory`. **Every mutation runs inside `using (await gate.EnterAsync())`** (`GameGate`, a global semaphore). That is how concurrent bids stay consistent, so keep new writes inside it.
- **State lives in the database** (the single `GameStates` row), so a restart mid-question is safe. Only the cached deadline is in memory. It is restored by `GameService.InitializeAsync()`, and `DeadlineService` ticks every 200 ms to close questions. `SubmitBidAsync` checks the clock itself, because the tick may lag.
- **Rule failures:** services throw `GameRuleException` with a message safe to show the user, and `GameRuleHubFilter` turns it into a `HubException`. The client strips the SignalR wrapper text (`core/game-connection.ts`).
- **Pluggable rules:** `IScoringPolicy` (`HighestBidScoringPolicy`: only the highest bid moves money, and balances never go below zero) and `IQuestionPicker`. When a verdict changes, `JudgeAsync` undoes the previous one using the stored `Bid.BalanceDelta`/`WalletDelta`, not the bid amount.
- **Schema:** there are no EF migrations. `Seeder.InitializeAsync` calls `EnsureCreated`, seeds from `server/seed/*.json` on a fresh database (detected by `GameStates` being empty), and `UpgradeAsync` adds columns to older databases with raw `ALTER TABLE`. Any model change needs a matching upgrade step, or the move to migrations. `HubSecurityTests` covers the upgrade from an older database.
- **Client:** components → stores (`HostStore`, `TeamStore`, `SetupStore`, all signal-based) → `GameConnection` (an abstract class, implemented by `GameHubService`, the only file that imports `@microsoft/signalr`). `core/models.ts` holds hand-written DTO types plus `ServerEvents`/`ServerMethods`, which type every hub message. Keep it in sync with `Game/Dtos.cs` and `SignalRGameNotifier`. On reconnect, a store resumes with its token and replaces its state with a fresh snapshot, so missed events are harmless.
- **Tests:** `GameServiceTests` builds `GameService` directly on in-memory SQLite with `FakeTimeProvider` and a recording notifier. `HubSecurityTests` runs the real app via `WebApplicationFactory<Program>`, with a temporary SQLite file and real SignalR clients.

## Deploy constraint

Game state and the timer live in one process, so it runs as a **single instance** (Azure App Service with WebSockets on). Don't design for scale-out without changing that.

## Repo tooling

`.claude/commands/` (`new-feature`, `add-tests`, `fix-bug`, `prepare-pr`, `deploy-plan`, `update-diagrams`) and `.claude/agents/` (`security-reviewer`, `test-writer`, `ui-reviewer`) carry out the workflow in `docs/standards/development-workflow.md`.
