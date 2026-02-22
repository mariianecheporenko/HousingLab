using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class Availability : Entity
{
    public int HousingId { get; set; }

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public virtual Housing Housing { get; set; } = null!;
}
