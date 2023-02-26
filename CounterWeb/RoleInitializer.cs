using CounterWeb.Models;
using Microsoft.AspNetCore.Identity;
using Task = System.Threading.Tasks.Task;

namespace CounterWeb
{
    public class RoleInitializer
    {
        public static async Task InitializeAsync(UserManager<UserIdentity> userManager, RoleManager<IdentityRole> roleManager)
        {
            //roleManager.DeleteAsync(await roleManager.FindByNameAsync("user"));
            if (await roleManager.FindByNameAsync("admin") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }
            if (await roleManager.FindByNameAsync("teacher") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("teacher"));
            }
            if (await roleManager.FindByNameAsync("student") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("student"));
            }
        }

    }
}
