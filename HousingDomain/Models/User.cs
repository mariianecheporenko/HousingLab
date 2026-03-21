using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HousingDomain.Models;

public partial class User : IdentityUser<int>
{
    [Required]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string Gender { get; set; } = null!;

    [Display(Name = "Хоче здавати житло")] 
    public bool WantsToBeOwner { get; set; }

    [Display(Name = "Підтверджений власник")]
    public bool IsOwnerApproved { get; set; } = false!;

    public string Role { get; set; } = null!;

    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
