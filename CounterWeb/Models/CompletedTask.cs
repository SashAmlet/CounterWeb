using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class CompletedTask
{
    public int CompletedTaskId { get; set; }

    public int TaskId { get; set; }

    public int? Grade { get; set; }

    public string? Solution { get; set; }

    public int? UserCourseId { get; set; }

    public virtual Task Task { get; set; } = null!;
}
