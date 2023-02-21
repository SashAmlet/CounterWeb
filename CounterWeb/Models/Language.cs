using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Language
{
    public int LanguageId { get; set; }

    public string? Ukranian { get; set; }

    public string? English { get; set; }

    public string? Russian { get; set; }

    public virtual Personalization? Personalization { get; set; }
}
