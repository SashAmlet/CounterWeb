using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public string? ZoomLink { get; set; }

    public virtual ICollection<Task> Tasks { get; } = new List<Task>();

    public virtual ICollection<UserCourse> UserCourses { get; } = new List<UserCourse>();
}
