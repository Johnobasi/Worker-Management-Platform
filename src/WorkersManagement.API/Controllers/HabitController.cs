using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HabitController(IHabitRepository habitService) : ControllerBase
    {
        private readonly IHabitRepository _habitService = habitService;

        [HttpPost("add")]
        public async Task<IActionResult> AddHabit([FromBody] AddHabitRequest request)
        {
            await _habitService.AddHabitAsync(new Habit
            {
                WorkerId = request.WorkerId,
                Type = request.Type,
                Notes = request.Notes,
                Amount = request.Amount
            });
            return Ok();
        }

        [HttpGet("{workerId}/type/{type}")]
        public async Task<IActionResult> GetHabitsByType(Guid workerId, HabitType type)
        {
            var habits = await _habitService.GetHabitsByTypeAsync(workerId, type);
            return Ok(habits);
        }

        //get habit types
        [HttpGet("types")]
        public async Task<IActionResult> GetHabitTypes(Guid workersId, HabitType types)
        {
            var habits = await _habitService.GetHabitsByTypeAsync(workersId, types);
            return Ok(habits);
        }
    }
}
