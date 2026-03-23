using System.ComponentModel.DataAnnotations;

namespace HousingInfrastructure.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введіть Email")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = null!;

    [Display(Name = "Запам'ятати мене?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}