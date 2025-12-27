using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BirdObservationWeb.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        // 1. Þekerim hafýzadaki tüm bilgileri (UserId, UserName vb.) siliyoruz
        HttpContext.Session.Clear();

        // 2. Kullanýcýyý tertemiz bir þekilde ana sayfaya geri yolluyoruz
        return RedirectToPage("/Index");
    }
}