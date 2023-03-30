using CounterWeb.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Validations
{
    public class CourseValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var course = (Course)validationContext.ObjectInstance;
            using (var dbContext = new CounterDbContext())
            {
                bool isNameUnique = !dbContext.Courses.Any(b => b.Name == course.Name && b.CourseId != course.CourseId);

                if (!isNameUnique)
                {
                    return new ValidationResult("The name '" + course.Name + "' is already taken.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
