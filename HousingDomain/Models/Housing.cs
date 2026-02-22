using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class Housing : Entity
{
    public string Address { get; set; } = null!;

    public string? City { get; set; }

    public decimal? Price { get; set; }

    public int? Rooms { get; set; }

    public decimal? Area { get; set; }

    public bool? IsAvailable { get; set; }

    public string? Description { get; set; }

    public int? OwnerId { get; set; }

    public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();

    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
