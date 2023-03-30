using CounterWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Validations.TaskValidation
{
    public class Name : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Validate the Name property
            var nameUnique = validationContext.ObjectType.GetProperty("Name");
            if (nameUnique != null)
            {
                var task = (Models.Task)validationContext.ObjectInstance;
                using (var dbContext = new CounterDbContext())
                {
                    bool isNameUnique = !dbContext.Tasks.Any(b => b.CourseId == task.CourseId && b.Name == task.Name && b.TaskId != task.TaskId);

                    if (!isNameUnique)
                    {
                        return new ValidationResult("The name '" + task.Name + "' is already taken.");
                    }
                }

            }

            return ValidationResult.Success;
        }
    }

}
