# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game with an Angular frontend and an ASP.NET Core (.NET 8) Web API backend. The backend owns all game rules, session state, move history, and the scoreboard; the Angular app is a thin client that renders whatever the backend returns.

## Project Overview

- Standard 3×3 board, two modes: **Two Player** and **Play Against Computer**.
- Full move validation, win/draw detection, winning-cell highlighting.
- Move history, Undo (mode-aware), Reset Game, and a session-level Scoreboard with its own Reset.
- Computer opponent plays O using a fixed priority: win → block → center → corner → any available cell.

## Tech Stack

| Layer    | Technology |
|----------|------------|
| Frontend | Angular 17 (standalone components) + TypeScript |
| Backend  | ASP.NET Core Web API (.NET 8) |
| API      | REST (JSON) |
| Storage  | In-memory (`ConcurrentDictionary`), per the problem statement's "in-memory is acceptable" note |
| Tests    | xUnit (backend), Jasmine/Karma (frontend) |

## Features Implemented

- [x] 3×3 clickable board, cells lock once played
- [x] Two Player mode with alternating turns; invalid moves don't change the turn
- [x] Win detection (rows, columns, diagonals) with winning-cell highlight
- [x] Draw detection
- [x] Reset Game (keeps scoreboard, starts a fresh session)
- [x] Move history (move number, player, cell position)
- [x] Undo Last Move, mode-aware (see Clarification 2 below)
- [x] Scoreboard (X wins / O wins / draws), counted once per completed game, with its own Reset Scoreboard
- [x] Computer Mode with the specified move-priority logic
- [x] Backend-owned game state — frontend only renders what the API returns
- [x] Backend unit tests covering all listed scenarios

## Project Structure

```
TicTacToe/
├── backend/
│   ├── TicTacToe.sln
│   ├── TicTacToe.Api/          ASP.NET Core Web API
│   │   ├── Controllers/        GamesController, ScoreboardController
│   │   ├── Models/              GameState, GameEngine (rules), Scoreboard, enums
│   │   ├── Services/            GameService (in-memory store + session logic)
│   │   ├── Dtos/                Request/response contracts
│   │   └── Program.cs
│   └── TicTacToe.Tests/        xUnit tests (engine + service)
└── frontend/
    └── src/app/
        ├── game-board/          Main game UI (component + template + styles + spec)
        ├── services/            GameService (HttpClient wrapper)
        └── models/              TypeScript interfaces mirroring the API DTOs
```

## How to Run the Backend Locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend/TicTacToe.Api
dotnet restore
dotnet run
```

The API starts at `http://localhost:5000` (see `Properties/launchSettings.json`) with Swagger UI at `http://localhost:5000/swagger` in Development mode. CORS is pre-configured to allow `http://localhost:4200` (the Angular dev server).

## How to Run the Frontend Locally

Requires [Node.js](https://nodejs.org) 18+ and the Angular CLI (`npm install -g @angular/cli`, or just use `npx`).

```bash
cd frontend
npm install
npm start        # ng serve, http://localhost:4200
```

The frontend calls the backend at `http://localhost:5000` (configured in `src/environments/environment.ts`). Start the backend first.

## API Endpoint Summary

| Method | Endpoint                     | Purpose                          |
|--------|-------------------------------|-----------------------------------|
| POST   | `/api/games`                  | Create a new game session (`{ "mode": "TwoPlayer" \| "VsComputer" }`) |
| GET    | `/api/games/{id}`             | Get current game state |
| POST   | `/api/games/{id}/moves`       | Submit a move (`{ "player": "X" \| "O", "cellIndex": 0-8 }`) |
| POST   | `/api/games/{id}/undo`        | Undo the last move (mode-aware) |
| POST   | `/api/games/{id}/reset`       | Reset the current game (scoreboard untouched) |
| GET    | `/api/scoreboard`             | Get the session scoreboard |
| POST   | `/api/scoreboard/reset`       | Reset the scoreboard to 0-0-0 |

**Game state response** includes: game id, board (9-cell array of `"X" | "O" | null`), current player, mode, status (`InProgress | Won | Draw`), winner, winning cells, `canUndo`, move history, and the scoreboard.

The backend rejects: moves outside the board, moves on an occupied cell, moves after game completion, and moves by the wrong player — each with a `400` and a descriptive message.

## How to Run Tests

**Backend:**
```bash
cd backend
dotnet test
```
Covers: valid/invalid moves, turn switching, row/column/diagonal wins, draw detection, reset, undo in both modes, scoreboard updates (including "only once per game"), computer move selection (win/block/center/corner priority), and rejecting moves after completion.

**Frontend:**
```bash
cd frontend
npm test
```
Covers component rendering and API interaction (move submission, guarding against clicks on filled cells), using Angular's `HttpClientTestingModule`.

## Design Decisions & Clarifications

**Clarification 1 — Backend State Ownership.** The Angular app holds no game rules. Every user action calls an API endpoint and re-renders the response; the board, turn, status, history, and scoreboard shown are always exactly what the backend returned.

**Clarification 2 — Scoreboard and Undo.** This solution uses **Option A: Disable Undo After Completion**. Once a game's status is `Won` or `Draw`, `POST /undo` returns `400` and the scoreboard entry for that game is final. This was chosen because it keeps the scoreboard's "count once" invariant trivial to reason about and test, rather than needing to reverse-and-recount scoreboard entries when a completed result is undone.

**Undo in Computer Mode.** If the last move was the computer's (O), Undo removes that move together with the preceding human (X) move, returning control to the human — per the problem statement's example. If the human's move ended the game before the computer replied (i.e. the last recorded move is X), Undo only removes that single move.

**State recomputation.** `GameState.RecomputeFromHistory()` rebuilds the board, current player, and status purely from the recorded `MoveHistory` after every Undo. This avoids any drift between "what moves were made" and "what the board/turn/status show."

**Scoreboard counting.** Each `GameState` tracks a `ScoreboardCounted` flag so a completed game can only increment the scoreboard once, even if the same finished state is fetched (`GET`) repeatedly.

**Enums as strings.** The API serializes enums (`Player`, `GameMode`, `GameStatus`) as strings (e.g. `"X"`, `"VsComputer"`, `"Won"`) for a readable JSON contract with the frontend.

## AI Tools & Prompt Summary

- Used an AI assistant to convert the problem statement into a concrete API contract and data model, generate the ASP.NET Core backend (controllers, service, game engine, DTOs) and xUnit tests, and scaffold the Angular standalone frontend (service, component, template/styles, a basic spec).
- Prompted for: the full problem statement (functional requirements, API scope, undo semantics, clarifications) and a request to "design and implement the ASP.NET backend and Angular frontend as per the requirement, including tests and documentation."
- Reviewed carefully / adjusted manually: the Undo semantics per mode (translating the two examples in the problem statement into exact logic), the "scoreboard counted once" flag design, the computer-move priority order and its unit tests, and the choice between Undo Option A vs B (documented above).
- Assumptions: `cellIndex` (0-8) is used as the move coordinate (row/column can be derived: `row = cellIndex / 3`, `col = cellIndex % 3`); a completed game is only countable once even across repeated GETs; Reset Game reuses the same `GameId` rather than issuing a new one.
- Trade-offs: in-memory storage (per the problem statement) means state is lost on backend restart — acceptable for a local review exercise; SQLite was not added since it wasn't required.

## Known Limitations

- State is in-memory only; restarting the backend clears all games and the scoreboard.
- No authentication/multi-user isolation — the scoreboard is a single session-level counter, as specified.
- No persistence layer (SQLite) is wired up, though the service layer is isolated enough to add one later.

## Future Improvements

- Add SQLite persistence for games/scoreboard across restarts.
- Add an "unbeatable" (minimax) computer difficulty option alongside the current priority-based one.
- Add Cypress/E2E tests covering full game flows through the UI.
- Add optimistic UI updates with rollback on API error, to reduce perceived latency on each move.
