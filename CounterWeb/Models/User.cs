﻿using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class User
{
    public int UserId { get; set; }

    public int? PersonalizationId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public virtual Personalization? Personalization { get; set; }

    public virtual ICollection<UserCourse>? UserCourses { get; } = new List<UserCourse>();
}
