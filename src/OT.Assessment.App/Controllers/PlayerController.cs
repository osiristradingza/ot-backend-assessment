using Microsoft.AspNetCore.Mvc;
using OT.Assessment.Shared.DTOs;
using OT.Assessment.Shared.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace OT.Assessment.App.Controllers
{
    /// <summary>
    /// Controller for handling player casino wager operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ICasinoService _casinoService;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(
            IRabbitMQPublisher rabbitMQPublisher, 
            ICasinoService casinoService,
            ILogger<PlayerController> logger)
        {
            _rabbitMQPublisher = rabbitMQPublisher;
            _casinoService = casinoService;
            _logger = logger;
        }

        /// <summary>
        /// Receives player casino wager events and publishes them to RabbitMQ queue
        /// </summary>
        /// <param name="wager">Casino wager data</param>
        /// <returns>Success response</returns>
        [HttpPost("casinowager")]
        [SwaggerResponse(200, "Casino wager received successfully")]
        [SwaggerResponse(400, "Invalid wager data")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> CasinoWager([FromBody] CasinoWagerDto wager)
        {
            try
            {
                _logger.LogInformation("Received casino wager: {WagerId}", wager.WagerId);
                
                // Validate the wager data
                if (string.IsNullOrEmpty(wager.WagerId) || 
                    string.IsNullOrEmpty(wager.AccountId) || 
                    string.IsNullOrEmpty(wager.Username))
                {
                    return BadRequest("Invalid wager data: Missing required fields");
                }

                // Publish to RabbitMQ
                await _rabbitMQPublisher.PublishCasinoWagerAsync(wager);
                
                _logger.LogInformation("Successfully published casino wager to queue: {WagerId}", wager.WagerId);
                
                return Ok(new { message = "Casino wager received successfully", wagerId = wager.WagerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process casino wager: {WagerId}", wager?.WagerId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Returns a paginated list of the latest casino wagers for a specific player
        /// </summary>
        /// <param name="playerId">Player account ID</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <returns>Paginated casino wagers</returns>
        [HttpGet("{playerId}/casino")]
        [SwaggerResponse(200, "Player casino wagers retrieved successfully", typeof(PlayerWagersResponseDto))]
        [SwaggerResponse(400, "Invalid player ID")]
        [SwaggerResponse(404, "Player not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetPlayerCasinoWagers(
            [FromRoute] string playerId, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] int page = 1)
        {
            try
            {
                if (!Guid.TryParse(playerId, out var accountId))
                {
                    return BadRequest("Invalid player ID format");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                _logger.LogInformation("Getting casino wagers for player: {PlayerId}, Page: {Page}, PageSize: {PageSize}", 
                    playerId, page, pageSize);

                var result = await _casinoService.GetPlayerWagersAsync(accountId, page, pageSize);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get casino wagers for player: {PlayerId}", playerId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Returns the top spending players based on total spending amount
        /// </summary>
        /// <param name="count">Number of top spenders to return (default: 10)</param>
        /// <returns>List of top spending players</returns>
        [HttpGet("topSpenders")]
        [SwaggerResponse(200, "Top spenders retrieved successfully", typeof(List<TopSpenderDto>))]
        [SwaggerResponse(400, "Invalid count parameter")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetTopSpenders([FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 1000) count = 10;

                _logger.LogInformation("Getting top {Count} spenders", count);

                var result = await _casinoService.GetTopSpendersAsync(count);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get top spenders");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
