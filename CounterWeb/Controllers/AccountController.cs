using CounterWeb.Models;
using CounterWeb.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CounterWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UserIdentity> _userManager;
        private readonly SignInManager<UserIdentity> _signInManager;
        private readonly CounterDbContext _context;
        public AccountController(UserManager<UserIdentity> userManager, SignInManager<UserIdentity> signInManager, CounterDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(ModelState.IsValid)
            {
                
                UserIdentity user = new UserIdentity
                { 
                    FirstName = model.FirstName, 
                    LastName = model.LastName, 
                    Email = model.Email, 
                    UserName = model.Email, 
                    //UserId = newUser.UserId 
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
