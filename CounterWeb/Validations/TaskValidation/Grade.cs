using CounterWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Validations.TaskValidation
{
    public class Grade : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Validate the MaxGrade property
            if (value != null)
            {
                int? maxGradeValue = (int?)value;
                if (maxGradeValue < 0)
                {
                    return new ValidationResult("Please enter a value greater than or equal to 0.");
                }
                if (maxGradeValue > int.MaxValue)
                {
                    return new ValidationResult("Please enter a value less than or equal to " + int.MaxValue);
                }
            }

            return ValidationResult.Success;
        }
    }


}
