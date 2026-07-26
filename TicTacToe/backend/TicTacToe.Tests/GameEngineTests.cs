using TicTacToe.Api.Models;
using Xunit;

namespace TicTacToe.Tests;

public class GameEngineTests
{
    private static Player?[] EmptyBoard() => new Player?[9];

    [Fact]
    public void CheckWinner_RowWin_IsDetected()
    {
        var board = EmptyBoard();
        board[0] = board[1] = board[2] = Player.X;

        var (winner, cells) = GameEngine.CheckWinner(board);

        Assert.Equal(Player.X, winner);
        Assert.Equal(new[] { 0, 1, 2 }, cells);
    }

    [Fact]
    public void CheckWinner_ColumnWin_IsDetected()
    {
        var board = EmptyBoard();
        board[0] = board[3] = board[6] = Player.O;

        var (winner, cells) = GameEngine.CheckWinner(board);

        Assert.Equal(Player.O, winner);
        Assert.Equal(new[] { 0, 3, 6 }, cells);
    }

    [Fact]
    public void CheckWinner_DiagonalWin_IsDetected()
    {
        var board = EmptyBoard();
        board[0] = board[4] = board[8] = Player.X;

        var (winner, cells) = GameEngine.CheckWinner(board);

        Assert.Equal(Player.X, winner);
        Assert.Equal(new[] { 0, 4, 8 }, cells);
    }

    [Fact]
    public void CheckWinner_NoWinner_ReturnsNull()
    {
        var board = EmptyBoard();
        board[0] = Player.X;
        board[1] = Player.O;

        var (winner, cells) = GameEngine.CheckWinner(board);

        Assert.Null(winner);
        Assert.Null(cells);
    }

    [Fact]
    public void IsBoardFull_DetectsDraw()
    {
        // X O X / X O O / O X X - full board, no winner
        var board = new Player?[]
        {
            Player.X, Player.O, Player.X,
            Player.X, Player.O, Player.O,
            Player.O, Player.X, Player.X
        };

        Assert.True(GameEngine.IsBoardFull(board));
        var (winner, _) = GameEngine.CheckWinner(board);
        Assert.Null(winner);
    }

    [Fact]
    public void GetComputerMove_TakesWinningMoveWhenAvailable()
    {
        // O has two in a row (row 1), should complete the win at index 2.
        var board = EmptyBoard();
        board[0] = Player.O;
        board[1] = Player.O;
        board[3] = Player.X;
        board[4] = Player.X;

        var move = GameEngine.GetComputerMove(board);

        Assert.Equal(2, move);
    }

    [Fact]
    public void GetComputerMove_BlocksOpponentWinWhenCannotWinItself()
    {
        // X threatens to win on the top row; O cannot win this turn, must block.
        var board = EmptyBoard();
        board[0] = Player.X;
        board[1] = Player.X;
        board[6] = Player.O;

        var move = GameEngine.GetComputerMove(board);

        Assert.Equal(2, move);
    }

    [Fact]
    public void GetComputerMove_TakesCenterWhenNoWinOrBlockNeeded()
    {
        var board = EmptyBoard();
        board[0] = Player.X;

        var move = GameEngine.GetComputerMove(board);

        Assert.Equal(GameEngine.CenterCell, move);
    }

    [Fact]
    public void GetComputerMove_TakesCornerWhenCenterTaken()
    {
        var board = EmptyBoard();
        board[4] = Player.X; // center already taken

        var move = GameEngine.GetComputerMove(board);

        Assert.Contains(move, GameEngine.Corners);
    }
}
