using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HousingDomain.Models;

public partial class BookingRequest : Entity
{
    [Required]
    [Display(Name = "Хто орендує?")]
    public int UserId { get; set; }

    [Required]
    [Display(Name = "Житло")]
    public int HousingId { get; set; }

    [Display(Name = "Орендовано з")] 
    public DateOnly DateFrom { get; set; }

    [Display(Name = "Орендовано до")]
    public DateOnly DateTo { get; set; }

    [Display(Name = "Статус")]
    public string? Status { get; set; }

    [Display(Name = "Житло")]
    public virtual Housing Housing { get; set; } = null!;

    [Display(Name = "Користувач")]
    public virtual User User { get; set; } = null!;
}
