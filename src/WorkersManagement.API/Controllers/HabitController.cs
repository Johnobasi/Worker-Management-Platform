using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage worker habits and completion tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HabitController(IHabitRepository habitService, 
        ILogger<HabitController> logger, IHabitCompletionRepository habitCompletionRepository
        ) : ControllerBase
    {
        private readonly IHabitRepository _habitService = habitService;
        private readonly ILogger<HabitController> _logger = logger;
        private readonly IHabitCompletionRepository _habitCompletionRepository = habitCompletionRepository;

        /// <summary>
        /// Get all habit types
        /// </summary>
        [HttpGet("habit-types")]
        [AllowAnonymous]
        public IActionResult GetHabitTypes()
        {
            var types = Enum.GetValues(typeof(HabitType))
                            .Cast<HabitType>()
                            .Select(t => new
                            {
                                Value = t.ToString(),
                                DisplayName = SplitCamelCase(t.ToString())
                            });

            return Ok(types);
        }

        private string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(input, "(\\B[A-Z])", " $1");
        }

        /// <summary>
        /// Get all habits
        /// </summary>
        /// <returns>List of all habits</returns>
        [HttpGet("get-habits")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> GetAllAsync()
        {
            var allHabits = await _habitService.GetAllHabit();
            _ = allHabits.Count();

            var habitProjections = allHabits.Select(delegate (Habit h)
            {
                return new
                {
                    HabitId = h.Id,
                    Type = h.Type,
                    Notes = h.Notes,
                    Amount = h.Amount,
                    CompletedAt = h.CompletedAt 
                };
            });
            return Ok(habitProjections);
        }

        /// <summary>
        /// Add a new habit
        /// </summary>
        /// <param name="request">Habit creation data</param>
        /// <returns>Add result</returns>
        [HttpPost("add-habit")]
        [AllowAnonymous]
        public async Task<IActionResult> AddHabit([FromBody] AddHabitRequest request)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }
                var habit = new Habit
                {
                    Id = Guid.NewGuid(),
                    Type = request.Type,
                    Notes = request.Notes,
                    Amount = request.Amount,
                    GivingType = request.GivingType,
                    CompletedAt = DateTime.UtcNow

                };
                var loggedInWorkerIdString = User.FindFirst("workerId")?.Value;

                if (string.IsNullOrWhiteSpace(loggedInWorkerIdString) ||
                    !Guid.TryParse(loggedInWorkerIdString, out Guid loggedInWorkerId))
                {
                    return Unauthorized("Invalid worker identity.");
                }

                await _habitService.AddHabitAsync(habit, loggedInWorkerId);
                return Ok("Habit successfully added.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding habit");
                return StatusCode(500, "An error occurred while adding the habit.");
            }
        }

        /// <summary>
        /// Update habit information
        /// </summary>
        /// <param name="id">Habit identifier</param>
        /// <param name="dto">Updated habit data</param>
        /// <returns>Update result</returns>
        [HttpPut("update-habit/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateHabitAsync(Guid id, [FromBody] UpdateHabitDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

                if (id != dto.Id)
                    return BadRequest("Habit ID mismatch.");

                var habit = await _habitService.GetHabitsByIdAsync(id);
                if (habit == null)
                    return NotFound($"Habit with ID '{id}' not found.");

                // Update values
                habit.Type= dto.Type;
                habit.CompletedAt = dto.CompletedAt;
                habit.Notes = dto.Notes;
                habit.Amount = dto.Amount;

                var result = await _habitService.UpdateHabitAsync(dto);
                if (!result)
                    return StatusCode(500, "Failed to update habit.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating habit with ID {HabitId}", id);
                return StatusCode(500, "An error occurred while updating the habit.");
            }
        }

        /// <summary>
        /// Delete a habit
        /// </summary>
        /// <param name="id">Habit identifier</param>
        /// <returns>Delete result</returns>
        [HttpDelete("delete-habit/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteHabitAsync(Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }
                var result = await _habitService.DeleteHabitAsync(id);
                return result
                    ? Ok("Habit successfully deleted.")
                    : StatusCode(500, "Failed to delete habit.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting habit with ID {HabitId}", id);
                return StatusCode(500, "An error occurred while deleting the habit.");
            }
        }

        /// <summary>
        /// Assign a habit to a worker
        /// </summary>
        /// <param name="dto">Assignment data</param>
        /// <returns>Assignment result</returns>
        [HttpPut("assign-worker")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignWorkerToHabit([FromBody] MapHabitToWorkerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

                var result = await _habitService.MapHabitToWorkerAsync(dto.HabitId, dto.WorkerId);
                if (!result)
                    return NotFound("Habit or Worker not found.");

                return Ok("Worker successfully assigned to habit.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning worker {WorkerId} to habit {HabitId}", dto.WorkerId, dto.HabitId);
                return StatusCode(500, "An error occurred while assigning the worker.");
            }
        }

        /// <summary>
        /// Mark habit as completed or incomplete
        /// </summary>
        /// <param name="workerId">Worker identifier</param>
        /// <param name="dto">Completion data</param>
        /// <returns>Completion result</returns>
        [HttpPost("mark-habit/{workerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> MarkHabitAsCompleted(Guid workerId, [FromBody] MarkHabitCompletionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

                var currentWorkerId = User.FindFirst("WorkerId")?.Value;
                var isAdmin = User.IsInRole(UserRole.Admin.ToString());

                if (!isAdmin && workerId.ToString() != currentWorkerId)
                    return Forbid("Workers can only mark their own habits.");

                var completion = new HabitCompletion
                {
                    HabitId = dto.HabitId,
                    WorkerId = workerId,
                    CompletedAt = DateTime.UtcNow,
                    IsCompleted = dto.IsCompleted
                };

                var result = await _habitCompletionRepository.AddCompletionAsync(completion);
                return Ok(new { Message = $"Habit marked as {(dto.IsCompleted ? "completed" : "incomplete")}", CompletionId = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking habit as completed for worker {workerId}");
                return StatusCode(500, "An error occurred while marking the habit as completed.");
            }
        }

        /// <summary>
        /// Logs a habit completion for a worker. 
        /// This is used when a worker performs a habit (e.g., clicks a button to join NLP Prayer).
        /// </summary>
        /// <param name="workerId">The unique identifier of the worker performing the habit.</param>
        /// <param name="habitType">The type of habit being logged (e.g., NLPPrayer, Fasting, Giving).</param>
        /// <returns>
        /// Returns 200 OK if the habit was successfully logged.
        /// </returns>
        /// <response code="200">Habit successfully logged.</response>
        /// <response code="400">WorkerId is missing or invalid.</response>
        /// <response code="500">An unexpected error occurred while logging the habit.</response>

        [HttpPost("log")]
        [AllowAnonymous]
        public async Task<IActionResult> LogHabit([FromQuery] Guid workerId, [FromQuery] HabitType habitType)
        {
            if (workerId == Guid.Empty)
                return BadRequest("Worker ID is required.");

            await _habitService.LogHabitAsync(workerId, habitType, "Logged from frontend button.");
            return Ok();
        }

        /// <summary>
        /// Get all giving types
        /// </summary>
        [HttpGet("giving-types")]
        [AllowAnonymous]
        public IActionResult GetGivingTypes()
        {
            var types = Enum.GetValues(typeof(GivingType))
                            .Cast<GivingType>()
                            .Select(gt => new { Id = (int)gt, Name = gt.ToString() })
                            .ToList();
            return Ok(types);
        }
    }
    /// <summary>
    /// Habit completion request data
    /// </summary>
    public record MarkHabitCompletionDto
    {
        [Required(ErrorMessage = "Habit ID is required.")]
        public Guid HabitId { get; init; }

        [Required(ErrorMessage = "Completion status is required.")]
        public bool IsCompleted { get; init; }
    }

    
}
