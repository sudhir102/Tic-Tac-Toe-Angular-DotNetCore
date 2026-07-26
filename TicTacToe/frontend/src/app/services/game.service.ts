import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateGameRequest,
  GameMode,
  GameStateResponse,
  MoveRequest,
  PlayerSymbol,
  Scoreboard
} from '../models/game.model';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly baseUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  createGame(mode: GameMode): Observable<GameStateResponse> {
    const body: CreateGameRequest = { mode };
    return this.http.post<GameStateResponse>(`${this.baseUrl}/games`, body);
  }

  getGame(gameId: string): Observable<GameStateResponse> {
    return this.http.get<GameStateResponse>(`${this.baseUrl}/games/${gameId}`);
  }

  makeMove(gameId: string, player: PlayerSymbol, cellIndex: number): Observable<GameStateResponse> {
    const body: MoveRequest = { player, cellIndex };
    return this.http.post<GameStateResponse>(`${this.baseUrl}/games/${gameId}/moves`, body);
  }

  undoLastMove(gameId: string): Observable<GameStateResponse> {
    return this.http.post<GameStateResponse>(`${this.baseUrl}/games/${gameId}/undo`, {});
  }

  resetGame(gameId: string): Observable<GameStateResponse> {
    return this.http.post<GameStateResponse>(`${this.baseUrl}/games/${gameId}/reset`, {});
  }

  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>(`${this.baseUrl}/scoreboard`);
  }

  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.baseUrl}/scoreboard/reset`, {});
  }
}
