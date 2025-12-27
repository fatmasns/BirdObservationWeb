namespace BirdObservationWeb.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.ComponentModel.DataAnnotations; // Þekerim bu lazým

public class RegisterModel : PageModel
{
    private readonly string _connectionString = default!;
    public RegisterModel(IConfiguration configuration) => _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    [BindProperty]
    public string FullName { get; set; } = "";

    [BindProperty]
    [EmailAddress(ErrorMessage = "Þekerim bu e-posta hiç gerçekçi durmuyor!")] // Format kontrolü
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. ADIM: Mail formatýný kontrol ediyoruz
        if (!ModelState.IsValid) return Page();

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 2. ADIM: E-posta zaten var mý kontrolü (Önleyici hamle!)
        using var cmdCheck = new NpgsqlCommand("SELECT COUNT(*) FROM observers WHERE email = @e", conn);
        cmdCheck.Parameters.AddWithValue("e", Email);
        var existingCount = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

        if (existingCount > 0)
        {
            ModelState.AddModelError("Email", "Bu e-posta zaten kapýlmýþ þekerim, baþka bir tane dene!");
            return Page();
        }

        // 3. ADIM: Yeni gözlemciyi kaydediyoruz
        string sql = "INSERT INTO observers (fullname, email, password, experiencelevel) VALUES (@n, @e, @p, 'Çaylak')";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("n", FullName);
        cmd.Parameters.AddWithValue("e", Email);
        cmd.Parameters.AddWithValue("p", Password);

        try
        {
            await cmd.ExecuteNonQueryAsync();
            return RedirectToPage("Login");
        }
        catch
        {
            ModelState.AddModelError("", "Kayýt sýrasýnda bir terslik oldu þekerim!");
            return Page();
        }
    }
}