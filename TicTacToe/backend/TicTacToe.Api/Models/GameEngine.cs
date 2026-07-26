namespace TicTacToe.Api.Models;

/// <summary>
/// Pure, stateless game rules: win detection and the computer opponent's move selection.
/// Kept separate from GameState so the rules are easy to unit test in isolation.
/// </summary>
public static class GameEngine
{
    // 3 rows, 3 columns, 2 diagonals
    public static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 },
        new[] { 3, 4, 5 },
        new[] { 6, 7, 8 },
        new[] { 0, 3, 6 },
        new[] { 1, 4, 7 },
        new[] { 2, 5, 8 },
        new[] { 0, 4, 8 },
        new[] { 2, 4, 6 },
    };

    public static readonly int CenterCell = 4;
    public static readonly int[] Corners = { 0, 2, 6, 8 };

    /// <summary>
    /// Returns the winner (if any) and the winning line's cell indices.
    /// </summary>
    public static (Player? Winner, int[]? WinningCells) CheckWinner(Player?[] board)
    {
        foreach (var line in Lines)
        {
            var a = board[line[0]];
            var b = board[line[1]];
            var c = board[line[2]];

            if (a is not null && a == b && b == c)
            {
                return (a, line);
            }
        }

        return (null, null);
    }

    public static bool IsBoardFull(Player?[] board) => board.All(c => c is not null);

    /// <summary>
    /// Chooses the computer's move (always plays as O) following the priority:
    /// 1. Win if possible
    /// 2. Block opponent's win
    /// 3. Take center
    /// 4. Take a corner
    /// 5. Take any available cell
    /// </summary>
    public static int GetComputerMove(Player?[] board)
    {
        var emptyCells = Enumerable.Range(0, 9).Where(i => board[i] is null).ToList();
        if (emptyCells.Count == 0)
        {
            throw new InvalidOperationException("No available cells for the computer to play.");
        }

        // 1. Win if possible
        var winningMove = FindWinningMove(board, Player.O, emptyCells);
        if (winningMove is not null) return winningMove.Value;

        // 2. Block opponent's win
        var blockingMove = FindWinningMove(board, Player.X, emptyCells);
        if (blockingMove is not null) return blockingMove.Value;

        // 3. Take center
        if (emptyCells.Contains(CenterCell)) return CenterCell;

        // 4. Take a corner
        var corner = Corners.FirstOrDefault(c => emptyCells.Contains(c), -1);
        if (corner != -1) return corner;

        // 5. Take any available cell
        return emptyCells[0];
    }

    private static int? FindWinningMove(Player?[] board, Player player, List<int> emptyCells)
    {
        foreach (var cell in emptyCells)
        {
            var trial = (Player?[])board.Clone();
            trial[cell] = player;
            var (winner, _) = CheckWinner(trial);
            if (winner == player)
            {
                return cell;
            }
        }

        return null;
    }
}
