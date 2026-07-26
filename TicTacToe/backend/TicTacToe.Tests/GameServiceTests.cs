using TicTacToe.Api.Models;
using TicTacToe.Api.Services;
using Xunit;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    [Fact]
    public void MakeMove_ValidMove_PlacesMarkAndSwitchesTurn()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        var updated = service.MakeMove(game.Id, Player.X, 0);

        Assert.Equal(Player.X, updated.Board[0]);
        Assert.Equal(Player.O, updated.CurrentPlayer); // turn switched
        Assert.Single(updated.MoveHistory);
    }

    [Fact]
    public void MakeMove_OnOccupiedCell_ThrowsAndDoesNotChangeTurn()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        service.MakeMove(game.Id, Player.X, 0);

        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.O, 0));

        var current = service.GetGame(game.Id);
        Assert.Equal(Player.O, current.CurrentPlayer); // unchanged by the invalid attempt
    }

    [Fact]
    public void MakeMove_ByWrongPlayer_IsRejected()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        // It's X's turn; O tries to move.
        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.O, 4));
    }

    [Fact]
    public void MakeMove_OutsideBoard_IsRejected()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.X, 9));
        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.X, -1));
    }

    [Fact]
    public void MakeMove_AfterGameCompletion_IsRejected()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        // X wins top row
        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 3);
        service.MakeMove(game.Id, Player.X, 1);
        service.MakeMove(game.Id, Player.O, 4);
        var finished = service.MakeMove(game.Id, Player.X, 2); // X completes row 0,1,2

        Assert.Equal(GameStatus.Won, finished.Status);
        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.O, 5));
    }

    [Fact]
    public void RowWin_IsDetectedAndScoreboardUpdatesOnce()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 3);
        service.MakeMove(game.Id, Player.X, 1);
        service.MakeMove(game.Id, Player.O, 4);
        var result = service.MakeMove(game.Id, Player.X, 2);

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal(Player.X, result.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, result.WinningCells);

        var scoreboard = service.GetScoreboard();
        Assert.Equal(1, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
    }

    [Fact]
    public void ColumnWin_IsDetected()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 1);
        service.MakeMove(game.Id, Player.X, 3);
        service.MakeMove(game.Id, Player.O, 2);
        var result = service.MakeMove(game.Id, Player.X, 6);

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal(Player.X, result.Winner);
    }

    [Fact]
    public void DiagonalWin_IsDetected()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 1);
        service.MakeMove(game.Id, Player.X, 4);
        service.MakeMove(game.Id, Player.O, 2);
        var result = service.MakeMove(game.Id, Player.X, 8);

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal(Player.X, result.Winner);
    }

    [Fact]
    public void Draw_IsDetectedWhenBoardFullWithNoWinner()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        // X O X
        // X O O
        // O X X
        var moves = new (Player, int)[]
        {
            (Player.X, 0), (Player.O, 1), (Player.X, 2),
            (Player.O, 4), (Player.X, 3), (Player.O, 5),
            (Player.X, 7), (Player.O, 6), (Player.X, 8)
        };

        GameState last = game;
        foreach (var (player, cell) in moves)
        {
            last = service.MakeMove(game.Id, player, cell);
        }

        Assert.Equal(GameStatus.Draw, last.Status);
        Assert.Null(last.Winner);

        var scoreboard = service.GetScoreboard();
        Assert.Equal(1, scoreboard.Draws);
    }

    [Fact]
    public void ResetGame_ClearsBoardAndHistoryButKeepsScoreboard()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 3);
        service.MakeMove(game.Id, Player.X, 1);
        service.MakeMove(game.Id, Player.O, 4);
        service.MakeMove(game.Id, Player.X, 2); // X wins, scoreboard now 1-0-0

        var reset = service.ResetGame(game.Id);

        Assert.All(reset.Board, cell => Assert.Null(cell));
        Assert.Empty(reset.MoveHistory);
        Assert.Equal(GameStatus.InProgress, reset.Status);
        Assert.Equal(Player.X, reset.CurrentPlayer);

        var scoreboard = service.GetScoreboard();
        Assert.Equal(1, scoreboard.XWins); // unchanged by Reset Game
    }

    [Fact]
    public void Undo_TwoPlayerMode_RemovesOnlyMostRecentMove()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 4);

        var afterUndo = service.UndoLastMove(game.Id);

        Assert.Single(afterUndo.MoveHistory);
        Assert.Equal(Player.X, afterUndo.MoveHistory[0].Player);
        Assert.Null(afterUndo.Board[4]);
        Assert.Equal(Player.X, afterUndo.Board[0]); // X's move preserved
        Assert.Equal(Player.O, afterUndo.CurrentPlayer); // O's turn again
    }

    [Fact]
    public void Undo_ComputerMode_RemovesComputerMoveAndPrecedingHumanMoveTogether()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.VsComputer);

        // Human plays X at 0; computer (O) auto-responds.
        var afterHumanMove = service.MakeMove(game.Id, Player.X, 0);
        Assert.Equal(2, afterHumanMove.MoveHistory.Count); // X move + computer's O move

        var afterUndo = service.UndoLastMove(game.Id);

        Assert.Empty(afterUndo.MoveHistory);
        Assert.All(afterUndo.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, afterUndo.CurrentPlayer); // back to X's turn
    }

    [Fact]
    public void Undo_IsDisabledWhenThereAreNoMovesToUndo()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Assert.Throws<InvalidMoveException>(() => service.UndoLastMove(game.Id));
    }

    [Fact]
    public void Undo_IsDisabledAfterGameCompletion()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 3);
        service.MakeMove(game.Id, Player.X, 1);
        service.MakeMove(game.Id, Player.O, 4);
        service.MakeMove(game.Id, Player.X, 2); // X wins

        Assert.Throws<InvalidMoveException>(() => service.UndoLastMove(game.Id));
    }

    [Fact]
    public void ComputerMode_ComputerMovesAutomaticallyAfterHumanMove()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.VsComputer);

        var result = service.MakeMove(game.Id, Player.X, 0);

        Assert.Equal(2, result.MoveHistory.Count);
        Assert.Equal(Player.O, result.MoveHistory[1].Player);
        Assert.Equal(Player.X, result.CurrentPlayer); // back to human after computer replies
    }

    [Fact]
    public void ComputerMode_ComputerNeverMovesOnceGameHasEnded()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.VsComputer);

        // Play human (X) moves one at a time, always into the first open cell,
        // letting the computer auto-respond after each, until the game ends.
        var state = service.GetGame(game.Id);
        while (state.Status == GameStatus.InProgress)
        {
            var nextEmptyCell = Enumerable.Range(0, 9).First(i => state.Board[i] is null);
            state = service.MakeMove(game.Id, Player.X, nextEmptyCell);
        }

        // Whatever the outcome, the game must be finished, and no further
        // move (from either side) should be accepted - i.e. the computer
        // cannot have snuck in a move after completion.
        Assert.NotEqual(GameStatus.InProgress, state.Status);
        Assert.Throws<InvalidMoveException>(() => service.MakeMove(game.Id, Player.O, 0));
    }

    [Fact]
    public void CreateGame_StartsWithEmptyBoardAndPlayerXFirst()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void ResetScoreboard_ClearsAllCounts()
    {
        var service = new GameService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        service.MakeMove(game.Id, Player.X, 0);
        service.MakeMove(game.Id, Player.O, 3);
        service.MakeMove(game.Id, Player.X, 1);
        service.MakeMove(game.Id, Player.O, 4);
        service.MakeMove(game.Id, Player.X, 2); // X wins

        service.ResetScoreboard();

        var scoreboard = service.GetScoreboard();
        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
    }
}
