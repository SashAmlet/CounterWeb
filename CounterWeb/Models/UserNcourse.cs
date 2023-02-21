using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class UserNcourse
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? CourseId { get; set; }

    public virtual Course? Course { get; set; }

    public virtual UserInfo? User { get; set; }
}
