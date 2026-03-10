using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HousingDomain.Models;

public partial class User : Entity
{
    [Required]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string Gender { get; set; } = null!;

    public string Role { get; set; } = "User";

    public string Username { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
