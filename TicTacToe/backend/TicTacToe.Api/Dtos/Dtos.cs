using TicTacToe.Api.Models;

namespace TicTacToe.Api.Dtos;

public class CreateGameRequest
{
    /// <summary>
    //TwoPlayer" or "VsComputer". Defaults to TwoPlayer
    ///</summary>
    public GameMode Mode { get; set; } = GameMode.TwoPlayer;
}

public class MoveRequest
{
    public Player Player { get; set; }
    public int CellIndex { get; set; }
}

public class MoveHistoryItemResponse
{
    public int MoveNumber { get; set; }
    public string Player { get; set; } = "";
    public int CellIndex { get; set; }
}

public class ScoreboardResponse
{
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }

    public static ScoreboardResponse FromModel(Scoreboard s) => new()
    {
        XWins = s.XWins,
        OWins = s.OWins,
        Draws = s.Draws
    };
}

public class GameStateResponse
{
    public Guid GameId { get; set; }
    public string?[] Board { get; set; } = new string?[9];
    public string CurrentPlayer { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Winner { get; set; }
    public int[]? WinningCells { get; set; }
    public bool CanUndo { get; set; }
    public List<MoveHistoryItemResponse> MoveHistory { get; set; } = new();
    public ScoreboardResponse Scoreboard { get; set; } = new();

    public static GameStateResponse FromModel(GameState g, Scoreboard scoreboard)
    {
        return new GameStateResponse
        {
            GameId = g.Id,
            Board = g.Board.Select(c => c?.ToString()).ToArray(),
            CurrentPlayer = g.CurrentPlayer.ToString(),
            Mode = g.Mode.ToString(),
            Status = g.Status.ToString(),
            Winner = g.Winner?.ToString(),
            WinningCells = g.WinningCells,
            CanUndo = g.Status == GameStatus.InProgress && g.MoveHistory.Count > 0,
            MoveHistory = g.MoveHistory.Select(m => new MoveHistoryItemResponse
            {
                MoveNumber = m.MoveNumber,
                Player = m.Player.ToString(),
                CellIndex = m.CellIndex
            }).ToList(),
            Scoreboard = ScoreboardResponse.FromModel(scoreboard)
        };
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = "";
}
