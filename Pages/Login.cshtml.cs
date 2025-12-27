using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;

namespace BirdObservationWeb.Pages;

public class LoginModel : PageModel
{
    private readonly string _connectionString = default!;

    public LoginModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT observerid, fullname FROM observers WHERE email = @e AND password = @p", conn);
        cmd.Parameters.AddWithValue("e", Email);
        cmd.Parameters.AddWithValue("p", Password);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            HttpContext.Session.SetInt32("UserId", reader.GetInt32(0));
            HttpContext.Session.SetString("UserName", reader.GetString(1));
            return RedirectToPage("Index");
        }

        ModelState.AddModelError("", "E-posta veya þifre hatalý þekerim!");
        return Page();
    }
}