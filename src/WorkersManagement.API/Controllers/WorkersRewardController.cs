using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// API controller for managing worker rewards
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkersRewardController : ControllerBase
    {
        private readonly ILogger<WorkersRewardController> _logger;
        private readonly IWorkerRewardRepository _workerRewardRepository;
        public WorkersRewardController(ILogger<WorkersRewardController> logger, IWorkerRewardRepository workerRewardRepository)
        {
            _logger = logger;
            _workerRewardRepository = workerRewardRepository;
        }

        /// <summary>
        /// gets the rewards for the authenticated worker.
        /// </summary>
        [HttpGet("get-rewards")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRewards()
        {
            try
            {
                // Get worker ID from claims
                var workerIdClaim = User.FindFirst("workerId")?.Value;
                if (!Guid.TryParse(workerIdClaim, out Guid workerId))
                    return Unauthorized("Invalid worker ID.");

                var rewards = await _workerRewardRepository.GetRewardsForWorkerAsync(workerId);
                return Ok(rewards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rewards.");
                return StatusCode(500, "Internal server error while retrieving rewards.");
            }
        }


        /// <summary>
        /// processes weekly rewards for all workers.
        /// </summary>
        [HttpGet("process-weekly-rewards")]
        public async Task<IActionResult> ProcessWeeklyRewards()
        {
            _logger.LogInformation("Starting weekly reward processing...");

            try
            {
                await _workerRewardRepository.ProcessAllRewardsAsync();

                _logger.LogInformation("Weekly reward processing completed.");
                return Ok("Weekly rewards processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during weekly reward processing.");
                return StatusCode(500, "Internal server error during reward processing.");
            }
        }
    }
}