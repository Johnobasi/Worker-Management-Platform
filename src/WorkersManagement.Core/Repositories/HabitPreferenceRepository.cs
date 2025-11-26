using Microsoft.EntityFrameworkCore;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class HabitPreferenceRepository : IHabitPreference
    {
        private readonly WorkerDbContext _context;


        public HabitPreferenceRepository(WorkerDbContext context)
        {
            _context = context;
        }


        public async Task SaveHabitsAsync(HabitSelectionRequest request)
        {
            var worker = await _context.Workers
            .Include(w => w.HabitPreferences)
            .FirstOrDefaultAsync(w => w.Id == request.WorkerId);


            if (worker == null)
                throw new InvalidOperationException("Worker not found");


            // Remove preferences not selected
            var toRemove = worker.HabitPreferences
            .Where(p => !request.SelectedHabits.Contains(p.HabitType))
            .ToList();


            _context.WorkerHabitPreferences.RemoveRange(toRemove);


            // Add new ones
            var existingTypes = worker.HabitPreferences.Select(p => p.HabitType).ToHashSet();
            var toAdd = request.SelectedHabits
            .Where(h => !existingTypes.Contains(h))
            .Select(h => new WorkerHabitPreference
            {
                WorkerId = worker.Id,
                HabitType = h
            })
            .ToList();

            _context.WorkerHabitPreferences.AddRange(toAdd);
            await _context.SaveChangesAsync();
        }

        public async Task<DashboardResponse> GetDashboardForWorkerAsync(Guid workerId)
        {
            var worker = await _context.Workers
             .Include(w => w.HabitPreferences)
             .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null)
                throw new Exception("Worker not found.");

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Get all logs for this worker
            var logs = await _context.Habits
                .Where(h => h.WorkerId == workerId)
                .ToListAsync();

            var result = new DashboardResponse
            {
                WorkerId = workerId,
                FirstName = worker.FirstName,
                Habits = new List<HabitDashboardItem>()
            };

            foreach (var pref in worker.HabitPreferences)
            {
                var habitLogs = logs.Where(l => l.Type == pref.HabitType).ToList();
                var monthlyLogs = habitLogs.Where(l => l.CompletedAt >= startOfMonth).ToList();

                int monthlyCount = monthlyLogs.Count;
                int allTimeCount = habitLogs.Count;

                decimal monthlyAmount = monthlyLogs.Sum(x => x.Amount ?? 0);
                decimal allTimeAmount = habitLogs.Sum(x => x.Amount ?? 0);

                int streak = CalculateStreak(habitLogs, pref.HabitType);

                string message = BuildMessage(
                    worker.FirstName,
                    pref.HabitType,
                    monthlyCount,
                    monthlyAmount,
                    allTimeCount,
                    allTimeAmount,
                    streak
                );

                result.Habits.Add(new HabitDashboardItem
                {
                    Habit = pref.HabitType,
                    MonthlyCount = monthlyCount,
                    AllTimeCount = allTimeCount,
                    MonthlyAmount = monthlyAmount,
                    AllTimeAmount = allTimeAmount,
                    Streak = streak,
                    Message = message
                });

                // Special handling for Giving
                if (pref.HabitType == HabitType.Giving)
                {
                    // Group by GivingType
                    var givingByType = habitLogs
                        .Where(h => h.GivingType.HasValue)
                        .GroupBy(h => h.GivingType.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(h => h.Amount ?? 0));

                    decimal totalGiving = givingByType.Values.Sum();

                    foreach (var kvp in givingByType)
                    {
                        result.GivingDetails.Add(new GivingDashboardItem
                        {
                            GivingType = kvp.Key,
                            MonthlyAmount = monthlyLogs.Where(l => l.GivingType == kvp.Key).Sum(l => l.Amount ?? 0),
                            AllTimeAmount = kvp.Value,
                            Message = $"Hi {worker.FirstName}, your total {kvp.Key} payment is £{kvp.Value:N2}"
                        });
                    }

                    // Add total giving
                    result.GivingDetails.Add(new GivingDashboardItem
                    {
                        GivingType = null,
                        MonthlyAmount = monthlyLogs.Sum(l => l.Amount ?? 0),
                        AllTimeAmount = totalGiving,
                        Message = $"Your total giving across all types is £{totalGiving:N2}"
                    });
                }
            }

            //results
            return result;
        }

        public async Task UpdateHabitPreferencesAsync(Guid workerId, UpdateHabitPreferencesRequest request)
        {
            // 1. Validate worker exists
            var worker = await _context.Workers
                .Include(w => w.HabitPreferences)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null)
                throw new KeyNotFoundException($"Worker with ID {workerId} was not found.");

            // 2. Clear all existing preferences
            var existingPreferences = await _context.WorkerHabitPreferences
                .Where(h => h.WorkerId == workerId)
                .ToListAsync();

            if (existingPreferences.Any())
            {
                _context.WorkerHabitPreferences.RemoveRange(existingPreferences);
            }

            // 3. Build new preferences from request
            var newPreferences = request.Habits
                .DistinctBy(h => h.Type)
                .Select(dto => new WorkerHabitPreference
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    HabitType = dto.Type,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // 4. Save new preferences
            if (newPreferences.Any())
            {
                await _context.WorkerHabitPreferences.AddRangeAsync(newPreferences);
            }

            await _context.SaveChangesAsync();
        }

        private int CalculateStreak(List<Habit> logs, HabitType type)
        {
            // Giving doesn't use streaks 
            if (type == HabitType.Giving)
                return 0;

            // Streak = consecutive days habit was completed
            var grouped = logs
                .Select(h => h.CompletedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (!grouped.Any())
                return 0;

            int streak = 1;
            DateTime current = grouped[0];

            for (int i = 1; i < grouped.Count; i++)
            {
                if (grouped[i] == current.AddDays(-1))
                {
                    streak++;
                    current = grouped[i];
                }
                else break;
            }

            return streak;
        }

        private string BuildMessage(
            string name,
            HabitType habit,
            int monthlyCount,
            decimal monthlyAmount,
            int allTimeCount,
            decimal allTimeAmount,
            int streak)
        {
            return habit switch
            {
                HabitType.Giving =>
                    $"Hi {name}, you have given £{monthlyAmount} this month. All-time giving: £{allTimeAmount}.",

                HabitType.Fasting =>
                    $"Hi {name}, you fasted {monthlyCount} days this month. Current streak: {streak}. Total: {allTimeCount} days.",

                HabitType.BibleStudy =>
                    $"Hi {name}, you studied the Bible {monthlyCount} times this month. Streak: {streak}. Total studies: {allTimeCount}.",

                HabitType.NLPPrayer =>
                    $"Hi {name}, you prayed {monthlyCount} times this month. Streak: {streak} days. All-time total: {allTimeCount} sessions.",

                HabitType.Devotionals =>
                    $"Hi {name}, you completed {monthlyCount} devotionals this month. Streak: {streak}. Total: {allTimeCount}.",

                _ => $"Hi {name}, great job staying consistent!"
            };
        }
    }
}
