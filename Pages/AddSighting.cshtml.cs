using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;

namespace BirdObservationWeb.Pages;

public class AddSightingModel : PageModel
{
    private readonly string _connectionString = default!;

    public AddSightingModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public List<SelectListItem> Locations { get; set; } = new();
    public List<SelectListItem> WeatherList { get; set; } = new();

    [BindProperty] public string BirdName { get; set; } = string.Empty;
    [BindProperty] public string BirdSpecies { get; set; } = string.Empty;
    [BindProperty] public string BirdColor { get; set; } = string.Empty;
    [BindProperty] public int Wingspan { get; set; }
    [BindProperty] public int SelectedLocationId { get; set; }
    [BindProperty] public int SelectedWeatherId { get; set; }

    // ✨ Yeni Eklenen Not Alanı
    [BindProperty] public string Notes { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("Login");

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        using var cmdLoc = new NpgsqlCommand("SELECT locationid, locationname FROM locations ORDER BY locationname ASC", conn);
        using var readerLoc = cmdLoc.ExecuteReader();
        while (readerLoc.Read()) Locations.Add(new SelectListItem { Value = readerLoc.GetInt32(0).ToString(), Text = readerLoc.GetString(1) });
        readerLoc.Close();

        using var cmdWea = new NpgsqlCommand("SELECT weatherid, weathertype FROM weather ORDER BY weathertype ASC", conn);
        using var readerWea = cmdWea.ExecuteReader();
        while (readerWea.Read()) WeatherList.Add(new SelectListItem { Value = readerWea.GetInt32(0).ToString(), Text = readerWea.GetString(1) });

        return Page();
    }

    public IActionResult OnPost()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("Login");

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();

        try
        {
            // 1. Kuşu Kaydet
            using var cmdBird = new NpgsqlCommand(@"
                INSERT INTO birds (commonname, species, color, wingspan) 
                VALUES (@n, @s, @c, @w) RETURNING birdid", conn);

            cmdBird.Parameters.AddWithValue("n", BirdName ?? (object)DBNull.Value);
            cmdBird.Parameters.AddWithValue("s", BirdSpecies ?? (object)DBNull.Value);
            cmdBird.Parameters.AddWithValue("c", BirdColor ?? (object)DBNull.Value);
            cmdBird.Parameters.AddWithValue("w", Wingspan);

            var result = cmdBird.ExecuteScalar();
            int newBirdId = (result != null) ? Convert.ToInt32(result) : 0;

            // 2. Gözlemi Kaydet (Notes sütunu eklendi!)
            using var cmdSighting = new NpgsqlCommand(@"
                INSERT INTO sightings (birdid, locationid, weatherid, observerid, sightingtime, notes) 
                VALUES (@bid, @lid, @wid, @oid, NOW(), @notes)", conn);

            cmdSighting.Parameters.AddWithValue("bid", newBirdId);
            cmdSighting.Parameters.AddWithValue("lid", SelectedLocationId);
            cmdSighting.Parameters.AddWithValue("wid", SelectedWeatherId);
            cmdSighting.Parameters.AddWithValue("oid", userId);
            // Boş bırakılırsa null gitsin diye kontrol ekledik şekerim
            cmdSighting.Parameters.AddWithValue("notes", (object?)Notes ?? DBNull.Value);
            cmdSighting.ExecuteNonQuery();

            // 3. Rütbe Sistemi
            using var cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM sightings WHERE observerid = @uid", conn);
            cmdCount.Parameters.AddWithValue("uid", userId);
            int total = Convert.ToInt32(cmdCount.ExecuteScalar());

            string newLevel = "Çaylak";
            if (total >= 75) newLevel = "Üstat";
            else if (total >= 45) newLevel = "Uzman";
            else if (total >= 15) newLevel = "Gözlemci";

            using var cmdUpdate = new NpgsqlCommand("UPDATE observers SET experiencelevel = @lvl WHERE observerid = @uid", conn);
            cmdUpdate.Parameters.AddWithValue("lvl", newLevel);
            cmdUpdate.Parameters.AddWithValue("uid", userId);
            cmdUpdate.ExecuteNonQuery();

            trans.Commit();
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            trans.Rollback();
            return Page();
        }
    }
}