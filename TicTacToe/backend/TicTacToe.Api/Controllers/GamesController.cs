using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Dtos;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly GameService _gameService;

    public GamesController(GameService gameService)
    {
        _gameService = gameService;
    }

    // POST /api/games
    [HttpPost]
    public ActionResult<GameStateResponse> CreateGame([FromBody] CreateGameRequest request)
    {
        var game = _gameService.CreateGame(request.Mode);
        var response = GameStateResponse.FromModel(game, _gameService.GetScoreboard());
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, response);
    }

    // GET /api/games/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> GetGame(Guid id)
    {
        try
        {
            var game = _gameService.GetGame(id);
            return Ok(GameStateResponse.FromModel(game, _gameService.GetScoreboard()));
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }

    // POST /api/games/{id}/moves
    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameStateResponse> MakeMove(Guid id, [FromBody] MoveRequest request)
    {
        try
        {
            var game = _gameService.MakeMove(id, request.Player, request.CellIndex);
            return Ok(GameStateResponse.FromModel(game, _gameService.GetScoreboard()));
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (InvalidMoveException ex)
        {
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
    }

    // POST /api/games/{id}/undo
    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameStateResponse> Undo(Guid id)
    {
        try
        {
            var game = _gameService.UndoLastMove(id);
            return Ok(GameStateResponse.FromModel(game, _gameService.GetScoreboard()));
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (InvalidMoveException ex)
        {
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
    }

    // POST /api/games/{id}/reset
    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameStateResponse> Reset(Guid id)
    {
        try
        {
            var game = _gameService.ResetGame(id);
            return Ok(GameStateResponse.FromModel(game, _gameService.GetScoreboard()));
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }
}
