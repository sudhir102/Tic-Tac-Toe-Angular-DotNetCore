import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { GameBoardComponent } from './game-board.component';
import { GameStateResponse } from '../models/game.model';
import { environment } from '../../environments/environment';

function emptyGame(overrides: Partial<GameStateResponse> = {}): GameStateResponse {
  return {
    gameId: 'game-1',
    board: Array(9).fill(null),
    currentPlayer: 'X',
    mode: 'TwoPlayer',
    status: 'InProgress',
    winner: null,
    winningCells: null,
    canUndo: false,
    moveHistory: [],
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides
  };
}

describe('GameBoardComponent', () => {
  let fixture: ComponentFixture<GameBoardComponent>;
  let component: GameBoardComponent;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [GameBoardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    fixture = TestBed.createComponent(GameBoardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    fixture.detectChanges(); // triggers ngOnInit -> createGame call
    const req = httpMock.expectOne(`${environment.apiUrl}/api/games`);
    req.flush(emptyGame());
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('renders a 3x3 board once a game is loaded', () => {
    const cells = fixture.nativeElement.querySelectorAll('.cell');
    expect(cells.length).toBe(9);
  });

  it('submits a move when an empty cell is clicked and renders the response', () => {
    const cells = fixture.nativeElement.querySelectorAll('.cell');
    cells[0].click();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/games/game-1/moves`);
    expect(req.request.body).toEqual({ player: 'X', cellIndex: 0 });

    req.flush(emptyGame({ board: ['X', null, null, null, null, null, null, null, null], currentPlayer: 'O' }));
    fixture.detectChanges();

    expect(component.game?.board[0]).toBe('X');
  });

  it('does not submit a move for an already-filled cell', () => {
    component.game = emptyGame({ board: ['X', null, null, null, null, null, null, null, null] });
    component.playCell(0);
    httpMock.expectNone(`${environment.apiUrl}/api/games/game-1/moves`);
  });
});
