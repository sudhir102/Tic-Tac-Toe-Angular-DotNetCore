using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Dtos;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public class ScoreboardController : ControllerBase
{
    private readonly GameService _gameService;

    public ScoreboardController(GameService gameService)
    {
        _gameService = gameService;
    }

    // GET /api/scoreboard
    [HttpGet]
    public ActionResult<ScoreboardResponse> Get()
    {
        return Ok(ScoreboardResponse.FromModel(_gameService.GetScoreboard()));
    }

    // POST /api/scoreboard/reset
    [HttpPost("reset")]
    public ActionResult<ScoreboardResponse> Reset()
    {
        _gameService.ResetScoreboard();
        return Ok(ScoreboardResponse.FromModel(_gameService.GetScoreboard()));
    }
}
