using System.Collections.Concurrent;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public class GameNotFoundException : Exception
{
    public GameNotFoundException(Guid id) : base($"Game '{id}' was not found.") { }
}

public class InvalidMoveException : Exception
{
    public InvalidMoveException(string message) : base(message) { }
}

/// <summary>
/// Owns all game sessions and the single session-level scoreboard.
/// In-memory only, as permitted by the problem statement. Registered as a
/// singleton so state survives across requests for the lifetime of the app.
/// </summary>
public class GameService
{
    private readonly ConcurrentDictionary<Guid, GameState> _games = new();
    private readonly object _scoreboardLock = new();
    private readonly Scoreboard _scoreboard = new();

    public GameState CreateGame(GameMode mode)
    {
        var game = new GameState { Mode = mode };
        _games[game.Id] = game;
        return game;
    }

    public GameState GetGame(Guid id)
    {
        if (!_games.TryGetValue(id, out var game))
        {
            throw new GameNotFoundException(id);
        }
        return game;
    }

    public Scoreboard GetScoreboard()
    {
        lock (_scoreboardLock)
        {
            // Return a snapshot copy so callers can't mutate internal state.
            return new Scoreboard
            {
                XWins = _scoreboard.XWins,
                OWins = _scoreboard.OWins,
                Draws = _scoreboard.Draws
            };
        }
    }

    public void ResetScoreboard()
    {
        lock (_scoreboardLock)
        {
            _scoreboard.XWins = 0;
            _scoreboard.OWins = 0;
            _scoreboard.Draws = 0;
        }
    }

    public GameState MakeMove(Guid gameId, Player player, int cellIndex)
    {
        var game = GetGame(gameId);

        lock (game)
        {
            if (game.Status != GameStatus.InProgress)
            {
                throw new InvalidMoveException("This game has already finished. Start a new game or reset.");
            }

            if (cellIndex < 0 || cellIndex > 8)
            {
                throw new InvalidMoveException("Move is outside the board.");
            }

            if (game.Board[cellIndex] is not null)
            {
                throw new InvalidMoveException("That cell is already occupied.");
            }

            if (player != game.CurrentPlayer)
            {
                throw new InvalidMoveException($"It is not {player}'s turn.");
            }

            if (game.Mode == GameMode.VsComputer && player != Player.X)
            {
                throw new InvalidMoveException("In Computer Mode, only the human player (X) submits moves.");
            }

            ApplyMove(game, player, cellIndex);
            TryCountScoreboard(game);

            // Let the computer respond automatically, only while the game is still open.
            if (game.Mode == GameMode.VsComputer && game.Status == GameStatus.InProgress)
            {
                var computerCell = GameEngine.GetComputerMove(game.Board);
                ApplyMove(game, Player.O, computerCell);
                TryCountScoreboard(game);
            }

            return game;
        }
    }

    public GameState UndoLastMove(Guid gameId)
    {
        var game = GetGame(gameId);

        lock (game)
        {
            // Clarification 2 - chosen approach: Option A (Disable Undo After Completion).
            // Once a game is Won or Draw, its result and the scoreboard are final.
            if (game.Status != GameStatus.InProgress)
            {
                throw new InvalidMoveException("Undo is not allowed after a game is completed.");
            }

            if (game.MoveHistory.Count == 0)
            {
                throw new InvalidMoveException("There are no moves to undo.");
            }

            var movesToRemove = 1;

            if (game.Mode == GameMode.VsComputer)
            {
                var lastMove = game.MoveHistory[^1];
                // If the computer (O) made the last move, undo removes that
                // computer move together with the preceding human (X) move,
                // so control returns to the human player.
                if (lastMove.Player == Player.O && game.MoveHistory.Count >= 2)
                {
                    movesToRemove = 2;
                }
                // Otherwise (last move was X, computer hasn't replied yet -
                // e.g. the human just completed the game) only that one move is removed.
            }

            for (var i = 0; i < movesToRemove; i++)
            {
                game.MoveHistory.RemoveAt(game.MoveHistory.Count - 1);
            }

            game.RecomputeFromHistory();

            return game;
        }
    }

    public GameState ResetGame(Guid gameId)
    {
        var game = GetGame(gameId);

        lock (game)
        {
            game.Board = new Player?[9];
            game.MoveHistory.Clear();
            game.CurrentPlayer = Player.X;
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = null;
            game.ScoreboardCounted = false;
            // Mode is preserved; scoreboard is untouched by design.
            return game;
        }
    }

    private static void ApplyMove(GameState game, Player player, int cellIndex)
    {
        game.Board[cellIndex] = player;
        game.MoveHistory.Add(new MoveRecord
        {
            MoveNumber = game.MoveHistory.Count + 1,
            Player = player,
            CellIndex = cellIndex
        });

        var (winner, winningCells) = GameEngine.CheckWinner(game.Board);
        if (winner is not null)
        {
            game.Status = GameStatus.Won;
            game.Winner = winner;
            game.WinningCells = winningCells;
        }
        else if (GameEngine.IsBoardFull(game.Board))
        {
            game.Status = GameStatus.Draw;
        }
        else
        {
            game.CurrentPlayer = player == Player.X ? Player.O : Player.X;
        }
    }

    private void TryCountScoreboard(GameState game)
    {
        if (game.ScoreboardCounted) return;
        if (game.Status == GameStatus.InProgress) return;

        lock (_scoreboardLock)
        {
            if (game.Status == GameStatus.Won)
            {
                if (game.Winner == Player.X) _scoreboard.XWins++;
                else if (game.Winner == Player.O) _scoreboard.OWins++;
            }
            else if (game.Status == GameStatus.Draw)
            {
                _scoreboard.Draws++;
            }
        }

        game.ScoreboardCounted = true;
    }
}
