using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using BirdObservationWeb.Models;

namespace BirdObservationWeb.Pages;

public class EditBirdModel : PageModel
{
    private readonly string _connectionString;

    [BindProperty]
    public Bird BirdToEdit { get; set; } = new();

    public List<SelectListItem> Locations { get; set; } = new();
    public List<SelectListItem> WeatherList { get; set; } = new();

    [BindProperty] public int SelectedLocationId { get; set; }
    [BindProperty] public int SelectedWeatherId { get; set; }

    [BindProperty] public string Notes { get; set; } = string.Empty;

    public EditBirdModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public IActionResult OnGet(int id)
    {
        // 🛡️ --- GÜVENLİK KAPISI: GİRİŞ KONTROLÜ ---
        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId == null)
        {
            // Giriş yapmadıysan hemen Login sayfasına şekerim!
            return RedirectToPage("/Login");
        }

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        // 1. Sahiplik Kontrolü (Veritabanından sahibini öğrenelim)
        int ownerId = 0;
        using (var cmdOwner = new NpgsqlCommand("SELECT observerid FROM sightings WHERE birdid = @id", conn))
        {
            cmdOwner.Parameters.AddWithValue("id", id);
            var result = cmdOwner.ExecuteScalar();
            ownerId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
        }

        // 🛡️ --- GÜVENLİK KAPISI: SAHİPLİK KONTROLÜ ---
        if (currentUserId != ownerId)
        {
            // Giriş yaptın ama bu kuş senin değilse ana sayfaya dön!
            return RedirectToPage("Index");
        }

        // 2. Kuş Bilgileri (Fiziksel)
        using (var cmd = new NpgsqlCommand("SELECT birdid, commonname, species, wingspan, endangeredstatus, color FROM Birds WHERE birdid = @id", conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                BirdToEdit = new Bird
                {
                    BirdID = reader.GetInt32(0),
                    CommonName = reader.GetString(1),
                    Species = reader.GetString(2),
                    WingSpan = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    EndangeredStatus = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Color = reader.IsDBNull(5) ? "" : reader.GetString(5)
                };
            }
        }

        // 3. Mevcut Gözlem ve NOT Bilgilerini Çekiyoruz
        using (var cmd = new NpgsqlCommand("SELECT locationid, weatherid, notes FROM sightings WHERE birdid = @id", conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                SelectedLocationId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                SelectedWeatherId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                Notes = reader.IsDBNull(2) ? "" : reader.GetString(2);
            }
        }

        FillDropdowns(conn);
        return Page();
    }

    public IActionResult OnPost()
    {
        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId == null) return RedirectToPage("Login");

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        // Sahiplik Kontrolünü OnPost'ta da yapalım ki hile olmasın
        using (var cmdCheck = new NpgsqlCommand("SELECT observerid FROM sightings WHERE birdid = @id", conn))
        {
            cmdCheck.Parameters.AddWithValue("id", BirdToEdit.BirdID);
            var result = cmdCheck.ExecuteScalar();
            var ownerId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

            if (currentUserId != ownerId) return RedirectToPage("Index");
        }

        using var trans = conn.BeginTransaction();
        try
        {
            // 1. Kuşu Güncelle
            using (var cmd = new NpgsqlCommand("CALL update_bird(@id, @name, @spec, @wing, @stat, @col)", conn))
            {
                cmd.Parameters.AddWithValue("id", BirdToEdit.BirdID);
                cmd.Parameters.AddWithValue("name", BirdToEdit.CommonName);
                cmd.Parameters.AddWithValue("spec", BirdToEdit.Species);
                cmd.Parameters.AddWithValue("wing", BirdToEdit.WingSpan);
                cmd.Parameters.AddWithValue("stat", BirdToEdit.EndangeredStatus ?? "");
                cmd.Parameters.AddWithValue("col", BirdToEdit.Color ?? "");
                cmd.ExecuteNonQuery();
            }

            // 2. Gözlemi ve NOTLARI Güncelle
            using (var cmd = new NpgsqlCommand("UPDATE sightings SET locationid = @lid, weatherid = @wid, notes = @notes WHERE birdid = @bid", conn))
            {
                cmd.Parameters.AddWithValue("lid", SelectedLocationId);
                cmd.Parameters.AddWithValue("wid", SelectedWeatherId);
                cmd.Parameters.AddWithValue("bid", BirdToEdit.BirdID);
                cmd.Parameters.AddWithValue("notes", (object?)Notes ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            trans.Commit();
            return RedirectToPage("Index");
        }
        catch
        {
            trans.Rollback();
            return Page();
        }
    }

    private void FillDropdowns(NpgsqlConnection conn)
    {
        using (var cmd = new NpgsqlCommand("SELECT locationid, locationname FROM locations ORDER BY locationname", conn))
        using (var reader = cmd.ExecuteReader())
            while (reader.Read()) Locations.Add(new SelectListItem { Value = reader.GetInt32(0).ToString(), Text = reader.GetString(1), Selected = reader.GetInt32(0) == SelectedLocationId });

        using (var cmd = new NpgsqlCommand("SELECT weatherid, weathertype FROM weather ORDER BY weathertype", conn))
        using (var reader = cmd.ExecuteReader())
            while (reader.Read()) WeatherList.Add(new SelectListItem { Value = reader.GetInt32(0).ToString(), Text = reader.GetString(1), Selected = reader.GetInt32(0) == SelectedWeatherId });
    }
}