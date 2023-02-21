using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class UserRegInfo
{
    public int RegInformationId { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;
}
