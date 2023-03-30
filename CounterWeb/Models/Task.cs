using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int CourseId { get; set; }

    [Remote(action: "IsNameUnique", controller: "Tasks", AdditionalFields = nameof(TaskId) + "," + nameof(Name) + "," + nameof(CourseId))]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "The field must be greater than or equal to 0.")]
    public int? MaxGrade { get; set; }
    public virtual ICollection<CompletedTask>? CompletedTasks { get; set; } = new List<CompletedTask>();
    public virtual Course? Course { get; set; } = null!;
}
