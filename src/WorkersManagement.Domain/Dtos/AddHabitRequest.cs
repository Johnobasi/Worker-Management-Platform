using System.ComponentModel.DataAnnotations;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos
{
    public class AddHabitRequest
    {
        [Required]
        public HabitType Type { get; set; }

        [Required(ErrorMessage = "Habit name is required.")]

        public string Notes { get; set; }


        public decimal? Amount { get; set; } // Only required for Giving
                                             // New field: Giving type (Tithe, Offering, FoodBank, KingdomDonations)
        public GivingType? GivingType { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == HabitType.Giving)
            {
                if (!Amount.HasValue || Amount <= 0)
                {
                    yield return new ValidationResult(
                        "Amount is required and must be greater than 0 for Giving type.",
                        new[] { nameof(Amount) });
                }

                if (!GivingType.HasValue)
                {
                    yield return new ValidationResult(
                        "GivingType is required when habit type is Giving.",
                        new[] { nameof(GivingType) });
                }
            }
        }
    }
}
