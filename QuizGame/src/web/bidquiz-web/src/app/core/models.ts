export type GameStatus = 'Idle' | 'Accepting' | 'Closed' | 'Revealed';
export type Verdict = 'Up' | 'Down';

export interface Question {
  id: number;
  order: number;
  category: string;
  text: string;
  imageUrl: string | null;
}

export interface QuestionSummary {
  id: number;
  order: number;
  asked: boolean;
}

export interface TeamScore {
  id: number;
  name: string;
  color: string;
  bidBalance: number;
  wallet: number;
  canBid: boolean;
  online: boolean;
}

export interface TeamOption {
  id: number;
  name: string;
}

export interface BidView {
  id: number;
  teamId: number;
  teamName: string;
  teamColor: string;
  answer: string;
  amount: number;
  verdict: Verdict | null;
}

export interface MyBid {
  answer: string;
  amount: number;
}

export interface TeamSnapshot {
  teamId: number;
  status: GameStatus;
  question: Question | null;
  endsAtUtc: string | null;
  serverNowUtc: string;
  myBid: MyBid | null;
  teams: TeamScore[];
}

export interface AdminSnapshot {
  status: GameStatus;
  question: Question | null;
  endsAtUtc: string | null;
  serverNowUtc: string;
  answeredCount: number;
  teamCount: number;
  questions: QuestionSummary[];
  teams: TeamScore[];
  bids: BidView[] | null;
  correctAnswer: string | null;
}

export interface TeamSession {
  token: string;
  snapshot: TeamSnapshot;
}

export interface AdminSession {
  token: string;
  snapshot: AdminSnapshot;
}

export interface ClientConfig {
  joinUrl: string;
  bidStep: number;
  questionSeconds: number;
}

export interface TeamInput {
  name: string;
  password: string | null;
}

export interface QuestionInput {
  category: string;
  text: string;
  correctAnswer: string;
  imageUrl: string | null;
}

export interface TeamSetup {
  id: number;
  name: string;
  color: string;
  playing: boolean;
}

export interface QuestionSetup extends QuestionInput {
  id: number;
  order: number | null;
}

export interface Setup {
  teams: TeamSetup[];
  questions: QuestionSetup[];
  questionsPerGame: number;
  teamsPerGame: number;
  maxTeams: number;
  minQuestionsPerGame: number;
  maxQuestionsPerGame: number;
  locked: boolean;
}

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
