using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Theme
{
    public int ThemeId { get; set; }

    public string? WhiteThemeSettings { get; set; }

    public string? DarkThemeSettings { get; set; }

    public virtual Personalization? Personalization { get; set; }
}
