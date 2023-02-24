using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? MaxGrade { get; set; }

    public virtual ICollection<CompletedTask>? CompletedTasks { get; set; } = new List<CompletedTask>();

    public virtual Course? Course { get; set; } = null!;
}
