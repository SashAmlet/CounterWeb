using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int CourseId { get; set; }

    [Validations.TaskValidation.Name]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    [Validations.TaskValidation.Grade]
    public int? MaxGrade { get; set; }
    public virtual ICollection<CompletedTask>? CompletedTasks { get; set; } = new List<CompletedTask>();
    public virtual Course? Course { get; set; } = null!;
}
