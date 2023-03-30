using CounterWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Validations
{
    public class CompletedTaskValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var completedTask = (CompletedTask)validationContext.ObjectInstance;
            using (var dbContext = new CounterDbContext())
            {
                var task = dbContext.Tasks.Where(b => b.TaskId == completedTask.TaskId).FirstOrDefault();
                if (task != null)
                {
                    completedTask.Task = task;
                }
            }
            if (completedTask.Grade < 0 || completedTask.Grade > completedTask.Task.MaxGrade)
            {
                return new ValidationResult("Grade must be between 0 and " + completedTask.Task.MaxGrade.ToString());
            }

            return ValidationResult.Success;
        }
    }
}
