using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HousingDomain.Models;

public partial class Housing : Entity
{
    [Required]
    [Display(Name ="Адреса")]
    public string Address { get; set; } = null!;
    
    [Display(Name = "Місто")]
    public string? City { get; set; }

    [Display(Name = "Ціна")]
    public decimal? Price { get; set; }

    [Display(Name = "Кількість кімнат")]
    public int? Rooms { get; set; }


    [Display(Name = "Площа")]
    public decimal? Area { get; set; }


    [Display(Name = "Чи доступно?")]
    public bool? IsAvailable { get; set; }

    [Display(Name = "Опис")]
    public string? Description { get; set; }


    [Display(Name = "Власник")]
    public int? OwnerId { get; set; }

    public virtual User? Owner { get; set; }

    public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();

    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
