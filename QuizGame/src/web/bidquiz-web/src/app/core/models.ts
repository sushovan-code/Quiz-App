import type { components } from './api/schema';

/**
 * The contract types, generated from the server's OpenAPI document (openapi.json, then
 * `npm run generate:api`). Don't write these by hand; change the C# type and regenerate.
 */
type Schemas = components['schemas'];

export type GameStatus = Schemas['TeamSnapshotResponse']['status'];
export type Verdict = NonNullable<Schemas['BidResponse']['verdict']>;

export type Question = Schemas['QuestionResponse'];
export type QuestionSummary = Schemas['QuestionSummaryResponse'];
export type TeamScore = Schemas['TeamScoreResponse'];
export type TeamOption = Schemas['TeamOptionResponse'];
export type BidView = Schemas['BidResponse'];
export type MyBid = Schemas['MyBidResponse'];
export type TeamSnapshot = Schemas['TeamSnapshotResponse'];
export type AdminSnapshot = Schemas['AdminSnapshotResponse'];
export type TeamSession = Schemas['TeamSessionResponse'];
export type AdminSession = Schemas['AdminSessionResponse'];
export type ClientConfig = Schemas['ClientConfigResponse'];
export type TeamInput = Schemas['SaveTeamRequest'];
export type QuestionInput = Schemas['SaveQuestionRequest'];
export type TeamSetup = Schemas['TeamSetupResponse'];
export type QuestionSetup = Schemas['QuestionSetupResponse'];
export type Setup = Schemas['SetupResponse'];

/** Everything the server pushes. */
export interface ServerEvents {
  QuestionStarted: (question: Question, endsAtUtc: string, serverNowUtc: string) => void;
  AnswerCountChanged: (answered: number, total: number) => void;
  TimeUp: () => void;
  BidsRevealed: (bids: BidView[]) => void;
  AnswerRevealed: (text: string) => void;
  ScoresUpdated: (teams: TeamScore[]) => void;
  TeamPresenceChanged: (teamId: number, online: boolean) => void;
  GameReset: () => void;
  QuestionBoardChanged: (questions: QuestionSummary[]) => void;
  RosterChanged: () => void;
}

/** Everything the client can ask of the server. */
export interface ServerMethods {
  JoinTeam: (teamId: number, password: string) => TeamSession;
  ResumeTeam: (token: string) => TeamSession;
  LeaveTeam: () => void;
  AdminLogin: (password: string) => AdminSession;
  ResumeAdmin: (token: string) => AdminSession;
  AdminLogout: (token: string) => void;
  StartQuestion: (questionId: number) => void;
  SubmitBid: (answer: string, amount: number) => MyBid;
  RevealBids: () => void;
  RevealAnswer: () => void;
  Judge: (bidId: number, verdict: Verdict) => BidView;
  ResetGame: () => void;
  GetSetup: () => Setup;
  AddTeam: (input: TeamInput) => Setup;
  UpdateTeam: (teamId: number, input: TeamInput) => Setup;
  DeleteTeam: (teamId: number) => Setup;
  AddQuestion: (input: QuestionInput) => Setup;
  UpdateQuestion: (questionId: number, input: QuestionInput) => Setup;
  DeleteQuestion: (questionId: number) => Setup;
  SetQuestionsPerGame: (count: number) => Setup;
  SetTeamsPerGame: (count: number) => Setup;
  DrawQuestions: () => Setup;
}

export function sortTeams(teams: TeamScore[]): TeamScore[] {
  return [...teams].sort(
    (a, b) => b.wallet - a.wallet || b.bidBalance - a.bidBalance || a.name.localeCompare(b.name),
  );
}
