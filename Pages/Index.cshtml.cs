using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using BirdObservationWeb.Models;

namespace BirdObservationWeb.Pages;

public class IndexModel : PageModel
{
    private readonly string _connectionString;

    public List<SightingView> Sightings { get; set; } = new();
    public List<LeaderboardEntry> TopObservers { get; set; } = new();

    // ✨ Şekerim, sadece son 4 notu tutacak yeni listemiz burada:
    public List<string> RecentNotes { get; set; } = new();

    public IndexModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public void OnGet(string? searchTerm, bool onlyMySightings = false)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        // --- 1. NORMAL LİSTELEME SORGUSU ---
        string sql = @"
            SELECT b.birdid, b.commonname, 
                   COALESCE(l.locationname, 'Konum Belirtilmedi'), 
                   COALESCE(w.weathertype, 'Belirtilmedi'), 
                   COALESCE(o.fullname, 'Anonim Gözlemci'), 
                   s.sightingtime, 
                   COALESCE(o.experiencelevel, 'Çaylak'), 
                   s.observerid,
                   s.notes
            FROM birds b
            INNER JOIN sightings s ON b.birdid = s.birdid
            LEFT JOIN locations l ON s.locationid = l.locationid
            LEFT JOIN weather w ON s.weatherid = w.weatherid
            LEFT JOIN observers o ON s.observerid = o.observerid
            WHERE b.commonname ILIKE @p1";

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (onlyMySightings && currentUserId != null)
        {
            sql += " AND s.observerid = @userId";
        }

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("p1", (object?)($"%{(searchTerm ?? "")}%") ?? DBNull.Value);

            if (onlyMySightings && currentUserId != null)
                cmd.Parameters.AddWithValue("userId", (object?)currentUserId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            Sightings.Clear();
            while (reader.Read())
            {
                Sightings.Add(new SightingView
                {
                    BirdId = reader.GetInt32(0),
                    BirdName = reader.IsDBNull(1) ? "Bilinmiyor" : reader.GetString(1),
                    LocationName = reader.GetString(2),
                    WeatherType = reader.GetString(3),
                    ObserverName = reader.GetString(4),
                    SightingTime = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                    ExperienceLevel = reader.GetString(6),
                    Notes = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }
        }

        // --- 🏆 2. LİDERLİK TABLOSU SORGUSU ---
        string leaderSql = @"
            SELECT o.fullname, o.experiencelevel, COUNT(s.sightingid) as total
            FROM observers o
            JOIN sightings s ON o.observerid = s.observerid
            GROUP BY o.fullname, o.experiencelevel
            ORDER BY total DESC
            LIMIT 5";

        using (var cmdLeader = new NpgsqlCommand(leaderSql, conn))
        {
            using var readerLeader = cmdLeader.ExecuteReader();
            TopObservers.Clear();
            while (readerLeader.Read())
            {
                TopObservers.Add(new LeaderboardEntry
                {
                    FullName = readerLeader.IsDBNull(0) ? "İsimsiz" : readerLeader.GetString(0),
                    Level = readerLeader.IsDBNull(1) ? "Çaylak" : readerLeader.GetString(1),
                    Count = readerLeader.GetInt32(2)
                });
            }
        }

        // --- 📝 3. SON 4 GÖZLEM NOTU SORGUSU ---
        string notesSql = @"
            SELECT notes 
            FROM sightings 
            WHERE notes IS NOT NULL AND notes <> '' 
            ORDER BY sightingtime DESC 
            LIMIT 4";

        using (var cmdNotes = new NpgsqlCommand(notesSql, conn))
        {
            using var readerNotes = cmdNotes.ExecuteReader();
            RecentNotes.Clear();
            while (readerNotes.Read())
            {
                RecentNotes.Add(readerNotes.GetString(0));
            }
        }
    }

    // ✨ Şekerim, silme işlemini burada güvenli hale getirdim:
    public IActionResult OnPostDelete(int id)
    {
        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId == null) return RedirectToPage("/Login");

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();

            // Veritabanı tutarlılığı için Transaction kullanıyoruz şekerim
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Önce bu kuşa bağlı gözlemleri (sightings) siliyoruz (Foreign Key kısıtlaması için)
                string deleteSightingsSql = "DELETE FROM sightings WHERE birdid = @id";
                using (var cmd1 = new NpgsqlCommand(deleteSightingsSql, conn))
                {
                    cmd1.Parameters.AddWithValue("id", id);
                    cmd1.ExecuteNonQuery();
                }

                // 2. Şimdi asıl kuşu silebiliriz
                string deleteBirdSql = "DELETE FROM birds WHERE birdid = @id";
                using (var cmd2 = new NpgsqlCommand(deleteBirdSql, conn))
                {
                    cmd2.Parameters.AddWithValue("id", id);
                    cmd2.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();
                throw; // Hata olursa işlemi geri alıyoruz
            }
        }

        return RedirectToPage();
    }
}

public class LeaderboardEntry
{
    public string FullName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int Count { get; set; }
}