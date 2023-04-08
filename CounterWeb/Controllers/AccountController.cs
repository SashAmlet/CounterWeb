using CounterWeb.Models;
using CounterWeb.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace CounterWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UserIdentity> _userManager;
        private readonly SignInManager<UserIdentity> _signInManager;
        private readonly CounterDbContext _context;
        private readonly SmtpClient _smtpClient;
        private readonly SmtpSettings _smtpSettings;
        public AccountController(UserManager<UserIdentity> userManager, SignInManager<UserIdentity> signInManager, CounterDbContext context, SmtpClient smtpClient, SmtpSettings smtpSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _smtpClient = smtpClient;
            _smtpSettings = smtpSettings;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(_userManager.Users.Where(a=>a.Email == model.Email).Count() > 0)
            {
                ModelState.AddModelError("", "Ця адреса електронної пошти вже зареєстрована, спробуйте увійти в обліковий запис, використовуючи цю адресу електронної пошти, або вкажіть іншу під час реєстрації.");
            }
            if (ModelState.IsValid)
            {

                UserIdentity user = new UserIdentity
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.Email,
                    EmailConfirmed = false
                };
                var result = await _userManager.CreateAsync(user, model.Password); // закидаємо користувача до бд-шки
                if (result.Succeeded)
                {
                    // Якщо запис успішно створено, то створюємо CounterDB
                    Language _language = new Language();
                    _context.Add(_language);
                    await _context.SaveChangesAsync();

                    Theme _theme = new Theme();
                    _context.Add(_theme);
                    await _context.SaveChangesAsync();

                    Personalization _personalization = new Personalization
                    {
                        Language = _language,
                        LanguageId = _language.LanguageId,
                        Theme = _theme,
                        ThemeId = _theme.ThemeId
                    };
                    _context.Add(_personalization);
                    await _context.SaveChangesAsync();

                    User newUser = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Personalization = _personalization,
                        PersonalizationId = _personalization.PersonalizationId
                    };
                    _context.Add(newUser);
                    await _context.SaveChangesAsync();

                    user.UserId = newUser.UserId;
                    await _userManager.UpdateAsync(user);

                    // установка кукі
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    //return RedirectToAction("Index", "Home");

                    //Підтвердження Email адреси

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);

                    // Надсилання email з посиланням на підтвердження

                    var message = new MailMessage();
                    message.From = new MailAddress(_smtpSettings.UserName);
                    message.To.Add(new MailAddress(model.Email));
                    message.Subject = "Підтвердіть ваш email";
                    message.Body = $"Підтвердіть ваш обліковий запис, перейшовши за посиланням: {callbackUrl}";
                    await _smtpClient.SendMailAsync(message);

                    TempData["Message"] = "Будь ласка, перевірте вашу почту (папку 'спам') та підтвердіть свій email.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var err in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return NotFound();
            }


            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Перевірити, що токен підтвердження збігається
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                TempData["Message"] = "Ваш email підтверджено!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Message"] = "Виникла проблема з підтвердженням email, будь ласка, сппробуйте ще раз.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result =
                    await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    // перевіряємо, чи належить URL додатку
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Неправильний логін чи (та) пароль");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // видаляємо аутентифікаційні куки
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
