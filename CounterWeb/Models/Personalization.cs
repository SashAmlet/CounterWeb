using System;
using System.Collections.Generic;

namespace CounterWeb.Models;

public partial class Personalization
{
    public int PersonalizationId { get; set; }

    public int LanguageId { get; set; }

    public int ThemeId { get; set; }

    public bool Notifications { get; set; }

    public virtual Language Language { get; set; } = null!;

    public virtual Theme Theme { get; set; } = null!;

    public virtual User? User { get; set; }
}
