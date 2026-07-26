namespace TicTacToe.Api.Models;

public class MoveRecord
{
    public int MoveNumber { get; set; }
    public Player Player { get; set; }
    public int CellIndex { get; set; } // 0-8
}

public class GameState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public GameMode Mode { get; set; }

    // 9 cells, null = empty
    public Player?[] Board { get; set; } = new Player?[9];

    public Player CurrentPlayer { get; set; } = Player.X;

    public GameStatus Status { get; set; } = GameStatus.InProgress;

    public Player? Winner { get; set; }

    public int[]? WinningCells { get; set; }

    public List<MoveRecord> MoveHistory { get; set; } = new();

    // Tracks whether this game's result has already been counted on the scoreboard,
    // so a completed game is only ever counted once.
    public bool ScoreboardCounted { get; set; }

    /// <summary>
    /// Rebuilds the board, current player, status, winner and winning cells
    /// purely from MoveHistory. Used after Undo so the derived state can
    /// never drift from the recorded moves.
    /// </summary>
    public void RecomputeFromHistory()
    {
        Board = new Player?[9];
        foreach (var move in MoveHistory)
        {
            Board[move.CellIndex] = move.Player;
        }

        var (winner, winningCells) = GameEngine.CheckWinner(Board);

        if (winner is not null)
        {
            Status = GameStatus.Won;
            Winner = winner;
            WinningCells = winningCells;
        }
        else if (Board.All(c => c is not null))
        {
            Status = GameStatus.Draw;
            Winner = null;
            WinningCells = null;
        }
        else
        {
            Status = GameStatus.InProgress;
            Winner = null;
            WinningCells = null;
        }

        // Whoever didn't make the last move goes next. If no moves yet, X starts.
        CurrentPlayer = MoveHistory.Count == 0
            ? Player.X
            : (MoveHistory[^1].Player == Player.X ? Player.O : Player.X);

        // A game that is no longer completed should be countable again if it
        // is completed a second time later.
        if (Status == GameStatus.InProgress)
        {
            ScoreboardCounted = false;
        }
    }
}
