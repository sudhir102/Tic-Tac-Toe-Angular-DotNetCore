export type PlayerSymbol = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveHistoryItem {
  moveNumber: number;
  player: PlayerSymbol;
  cellIndex: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameStateResponse {
  gameId: string;
  board: (PlayerSymbol | null)[];
  currentPlayer: PlayerSymbol;
  mode: GameMode;
  status: GameStatus;
  winner: PlayerSymbol | null;
  winningCells: number[] | null;
  canUndo: boolean;
  moveHistory: MoveHistoryItem[];
  scoreboard: Scoreboard;
}

export interface CreateGameRequest {
  mode: GameMode;
}

export interface MoveRequest {
  player: PlayerSymbol;
  cellIndex: number;
}
