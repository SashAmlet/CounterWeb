using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Course
{
    public int CourseId { get; set; }

    [Remote(action: "IsNameUnique", controller: "Courses", AdditionalFields = nameof(Name))]
    public string Name { get; set; } = null!;

    public string? ZoomLink { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
}
