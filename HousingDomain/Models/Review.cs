using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class Review : Entity
{
    public int UserId { get; set; }

    public int HousingId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public virtual Housing Housing { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
