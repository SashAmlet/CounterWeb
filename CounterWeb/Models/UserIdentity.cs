using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CounterWeb.Models;

public partial class UserIdentity: IdentityUser
{
    public int UserId { get; set; }

    public int? PersonalizationId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public virtual Personalization? Personalization { get; set; }

    public virtual ICollection<UserCourse>? UserCourses { get; } = new List<UserCourse>();
}
