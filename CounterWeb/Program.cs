using CounterWeb;
using CounterWeb.Models;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// зв'язування БДшечки з моделями
builder.Services.AddDbContext<CounterDbContext>(option => option.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ));
builder.Services.AddControllersWithViews();

// зв'язування БДшечки з моделями
builder.Services.AddDbContext<IdentityContext>(option => option.UseSqlServer(
    builder.Configuration.GetConnectionString("IdentityConnection")
    ));
builder.Services.AddControllersWithViews();
builder.Services.AddIdentity<UserIdentity, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Tokens.ProviderMap.Add("Default", new TokenProviderDescriptor(
            typeof(IUserTwoFactorTokenProvider<IdentityUser>)));// Реєстрація провайдера токена двофакторної автентифікації за замовчуванням
})
    .AddEntityFrameworkStores<IdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.AllowedUserNameCharacters = options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz" +
                                             "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                             "абвгдеєжзиіїйклмнопрстуфхцчшщьюя" +
                                             "АБВГДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ" +
                                             "123456789-._@";
});
//

//
var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

var smtpSettings = configuration.GetSection("SmtpSettings")
    .Get<SmtpSettings>();

// Регистрация настроек SMTP-хоста
builder.Services.AddSingleton(smtpSettings);

// Регистрация клиента для отправки электронной почты
var smtpClient = new SmtpClient
{
    Host = smtpSettings.Host,
    Port = smtpSettings.Port,
    EnableSsl = smtpSettings.EnableSsl,
    UseDefaultCredentials = false,
    DeliveryMethod = SmtpDeliveryMethod.Network,
    Credentials = new NetworkCredential(smtpSettings.UserName, smtpSettings.Password)
};
builder.Services.AddSingleton(smtpClient);

//var client = new SmtpClient();

//
var app = builder.Build();

// Задати початкові ролі
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<UserIdentity>>();
        var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleInitializer.InitializeAsync(userManager, rolesManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database." + DateTime.Now.ToString());
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication(); // підключення автентифікації

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
