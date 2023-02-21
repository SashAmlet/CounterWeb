using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class UserInfo
{
    public int UserId { get; set; }

    public int? RegInfoId { get; set; }

    public int? PersonalizationId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? EmailAddr { get; set; }

    public virtual Personalization? Personalization { get; set; }

    public virtual ICollection<UserNcourse> UserNcourses { get; } = new List<UserNcourse>();
}
