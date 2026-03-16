using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class Profile : Entity
{
    public int UserId { get; set; }

    public string NoiseLevel { get; set; } = null!;

    public string SleepMode { get; set; } = null!;

    public bool Pets { get; set; }

    public string Guests { get; set; } = null!;

    public string CleanLevel { get; set; } = null!;

    public string Smoking { get; set; } = null!;

    public string PreferredGender { get; set; } = null!;


    public virtual User User { get; set; } = null!;
}
