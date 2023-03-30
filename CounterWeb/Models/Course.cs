using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CounterWeb.Models;

public partial class Course
{
    public int CourseId { get; set; }

    [Remote(action: "IsNameUnique", controller: "Courses", AdditionalFields = "CourseId,Name")]
    public string Name { get; set; } = null!;

    [RegularExpression(@"^(https?|ftp)://[^\s/$.?#].[^\s]*$")]
    public string? ZoomLink { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
}
