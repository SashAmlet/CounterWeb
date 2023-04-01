using CounterWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Validations
{
    public class CompletedTaskValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {

            var completedTask = (CompletedTask)validationContext.ObjectInstance;
            int? grade = (int?)value;
            using (var dbContext = new CounterDbContext())
            {
                var task = dbContext.Tasks.Where(b => b.TaskId == completedTask.TaskId).FirstOrDefault();
                if (task != null)
                {
                    completedTask.Task = task;
                }
            }
            if ( grade < 0 || grade > completedTask.Task?.MaxGrade)
            {
                return new ValidationResult("Grade must be between 0 and " + completedTask.Task?.MaxGrade.ToString());
            }

            return ValidationResult.Success;
        }

        /*protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            int? completedTaskId = (int?)value;
            Models.CompletedTask? completedTask;

            using (var dbContext = new CounterDbContext())
            {
                completedTask = dbContext.CompletedTasks.Where(b => b.CompletedTaskId == completedTaskId).FirstOrDefault();
            }
            if (completedTask == null)
            {
                return new ValidationResult("Value is not a CompletedTask object.");
            }

            var task = completedTask.Task;
            if (task == null)
            {
                using (var dbContext = new CounterDbContext())
                {
                    task = dbContext.Tasks.Where(b => b.TaskId == completedTask.TaskId).FirstOrDefault();
                    if (task != null)
                    {
                        completedTask.Task = task;
                    }
                }
            }

            if (completedTask.Grade < 0 || completedTask.Grade > task.MaxGrade)
            {
                return new ValidationResult("Grade must be between 0 and " + task.MaxGrade.ToString());
            }

            return ValidationResult.Success;
        }*/
    }
}

