import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { GameService } from '../services/game.service';
import { GameMode, GameStateResponse, PlayerSymbol } from '../models/game.model';

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-board.component.html',
  styleUrls: ['./game-board.component.css']
})
export class GameBoardComponent implements OnInit {
  game: GameStateResponse | null = null;
  errorMessage: string | null = null;
  loading = false;

  readonly cellIndexes = Array.from({ length: 9 }, (_, i) => i);

  constructor(private gameService: GameService) {}

  ngOnInit(): void {
    this.startNewGame('TwoPlayer');
  }

  startNewGame(mode: GameMode): void {
    this.loading = true;
    this.errorMessage = null;
    this.gameService.createGame(mode).subscribe({
      next: (game) => {
        this.game = game;
        this.loading = false;
      },
      error: () => this.handleError('Could not start a new game. Is the backend running?')
    });
  }

  playCell(cellIndex: number): void {
    if (!this.game) return;
    if (this.game.status !== 'InProgress') return;
    if (this.game.board[cellIndex] !== null) return;

    // In Computer Mode only the human (X) submits moves; O is played by the backend.
    const player: PlayerSymbol = this.game.currentPlayer;
    if (this.game.mode === 'VsComputer' && player !== 'X') return;

    this.errorMessage = null;
    this.gameService.makeMove(this.game.gameId, player, cellIndex).subscribe({
      next: (game) => (this.game = game),
      error: (err) => this.handleError(this.extractMessage(err, 'That move was not valid.'))
    });
  }

  undo(): void {
    if (!this.game || !this.game.canUndo) return;
    this.errorMessage = null;
    this.gameService.undoLastMove(this.game.gameId).subscribe({
      next: (game) => (this.game = game),
      error: (err) => this.handleError(this.extractMessage(err, 'Undo is not available right now.'))
    });
  }

  resetGame(): void {
    if (!this.game) return;
    this.errorMessage = null;
    this.gameService.resetGame(this.game.gameId).subscribe({
      next: (game) => (this.game = game),
      error: () => this.handleError('Could not reset the game.')
    });
  }

  resetScoreboard(): void {
    if (!this.game) return;
    this.errorMessage = null;
    this.gameService.resetScoreboard().subscribe({
      next: () => this.gameService.getGame(this.game!.gameId).subscribe((g) => (this.game = g)),
      error: () => this.handleError('Could not reset the scoreboard.')
    });
  }

  switchMode(mode: GameMode): void {
    if (this.game?.mode === mode) return;
    this.startNewGame(mode);
  }

  rowOf(cellIndex: number): number {
    return Math.floor(cellIndex / 3) + 1;
  }

  colOf(cellIndex: number): number {
    return (cellIndex % 3) + 1;
  }

  isWinningCell(cellIndex: number): boolean {
    return !!this.game?.winningCells?.includes(cellIndex);
  }

  statusMessage(): string {
    if (!this.game) return '';
    if (this.game.status === 'Won') return `Player ${this.game.winner} wins!`;
    if (this.game.status === 'Draw') return "It's a draw!";
    return `Player ${this.game.currentPlayer}'s turn`;
  }

  private handleError(message: string): void {
    this.errorMessage = message;
    this.loading = false;
  }

  private extractMessage(err: unknown, fallback: string): string {
    const httpErr = err as { error?: { message?: string } };
    return httpErr?.error?.message ?? fallback;
  }
}
