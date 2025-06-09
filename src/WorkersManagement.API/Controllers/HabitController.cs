using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HabitController(IHabitRepository habitService, ILogger<HabitController> logger, IHabitCompletionRepository habitCompletionRepository) : ControllerBase
    {
        private readonly IHabitRepository _habitService = habitService;
        private readonly ILogger<HabitController> _logger = logger;
        private readonly IHabitCompletionRepository _habitCompletionRepository = habitCompletionRepository;

        [HttpGet("get-habits")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> GetAllAsync()
        {
            var allHabits = await _habitService.GetAllHabit();
            _ = allHabits.Count();
            return Ok(allHabits);
        }

        [HttpPost("add-habit")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> AddHabit([FromBody] AddHabitRequest request)
        {

            try
            {
                var habit = new Habit
                {
                    Type = request.Type,
                    Notes = request.Notes,
                    Amount = request.Amount,
                };

                await _habitService.AddHabitAsync(habit);
                return Ok("Habit successfully added.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding habit");
                return StatusCode(500, "An error occurred while adding the habit.");
            }
        }

        [HttpPut("update-habit/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateHabitAsync(Guid id, [FromBody] UpdateHabitDto dto)
        {
            try
            {
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

        [HttpDelete("delete-habit/{id}")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> DeleteHabitAsync(DeleteHabitDto id)
        {
            try
            {
                var result = await _habitService.DeleteHabitAsync(id);
                if (!result)
                    return NotFound($"Habit with ID '{id}' not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting habit with ID {HabitId}", id);
                return StatusCode(500, "An error occurred while deleting the habit.");
            }
        }

        [HttpPut("assign-worker")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> AssignWorkerToHabit([FromBody] MapHabitToWorkerDto dto)
        {
            try
            {
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

        [HttpPost("mark-habit/{workerId}")]
        public async Task<IActionResult> MarkHabitAsCompleted(Guid workerId, [FromBody] MarkHabitCompletionDto dto)
        {
            try
            {
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
    }
    public record MarkHabitCompletionDto
        {
        public Guid HabitId { get; init; }
        public bool IsCompleted { get; init; }
    }

    
}
