using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousingDomain.Models;

public partial class Profile : Entity
{
    [Column("User_Id")]
    public int UserId { get; set; }

    [Display(Name = "Рівень шуму")]
    public string NoiseLevel { get; set; } = null!;

    [Display(Name = "Режим сну")]
    public string SleepMode { get; set; } = null!;

    [Display(Name = "Домашні улюбленці")]
    public bool Pets { get; set; }
    
    [Display(Name = "Гості")] 
    public string Guests { get; set; } = null!;

    [Display(Name = "Частота прибирання")]
    public string CleanLevel { get; set; } = null!;

    [Display(Name = "Куріння")]
    public string Smoking { get; set; } = null!;

    [Display(Name = "Бажаний гендер сусідів")]
    public string PreferredGender { get; set; } = null!;


    public virtual User? User { get; set; }
}
