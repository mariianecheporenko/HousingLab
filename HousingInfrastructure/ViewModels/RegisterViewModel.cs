using System.ComponentModel.DataAnnotations;

namespace HousingInfrastructure.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введіть Email")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введіть ім'я")]
    [Display(Name = "Ім'я")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Підтвердіть пароль")]
    [Compare("Password", ErrorMessage = "Паролі не співпадають")]
    [DataType(DataType.Password)]
    [Display(Name = "Підтвердження пароля")]
    public string PasswordConfirm { get; set; } = null!;

    [Required(ErrorMessage = "Вкажіть дату народження")]
    [Display(Name = "Дата народження")]
    public DateOnly BirthDate { get; set; }

    [Required(ErrorMessage = "Вкажіть стать")]
    [Display(Name = "Стать")]
    public string Gender { get; set; } = null!;

    [Display(Name = "Я хочу здавати житло (стати власником)")]
    public bool WantsToBeOwner { get; set; }
}