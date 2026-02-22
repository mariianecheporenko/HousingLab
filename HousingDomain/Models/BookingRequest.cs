using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class BookingRequest : Entity
{
    public int UserId { get; set; }

    public int HousingId { get; set; }

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public string? Status { get; set; }

    public virtual Housing Housing { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
