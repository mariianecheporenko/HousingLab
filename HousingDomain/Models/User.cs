using System;
using System.Collections.Generic;

namespace HousingDomain.Models;

public partial class User : Entity
{
    public string Email { get; set; } = null;

    public string Name { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string Gender { get; set; } = null!;

    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
