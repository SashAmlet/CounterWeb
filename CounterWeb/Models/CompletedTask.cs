using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Proxies;
using System.ComponentModel.DataAnnotations.Schema;

namespace CounterWeb.Models;

public partial class CompletedTask
{
    public int CompletedTaskId { get; set; }

    public int TaskId { get; set; }
    [Validations.CompletedTaskValidation]
    public int? Grade { get; set; }
    public string? Solution { get; set; }
    public int? UserCourseId { get; set; }

    public virtual Task? Task { get; set; } = null!;
}
