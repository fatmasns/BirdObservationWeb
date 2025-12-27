namespace BirdObservationWeb.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using BirdObservationWeb.Models;

public class DetailsModel : PageModel
{
    private readonly string _connectionString;
    public DetailsModel(IConfiguration configuration) => _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public Bird BirdDetails { get; set; } = new();
    public string LocationName { get; set; } = "";
    public string WeatherType { get; set; } = "";
    public string ObserverName { get; set; } = "";
    // ✨ Notu tutmak için yeni alanımız
    public string Notes { get; set; } = "";

    public void OnGet(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        // Şekerim, sorgunun sonuna 's.notes' sütununu ekledik!
        string sql = @"
            SELECT b.birdid, b.commonname, b.species, b.wingspan, b.endangeredstatus, b.color,
                   l.locationname, w.weathertype, o.fullname, s.notes
            FROM birds b
            LEFT JOIN sightings s ON b.birdid = s.birdid
            LEFT JOIN locations l ON s.locationid = l.locationid
            LEFT JOIN weather w ON s.weatherid = w.weatherid
            LEFT JOIN observers o ON s.observerid = o.observerid
            WHERE b.birdid = @id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            BirdDetails = new Bird
            {
                BirdID = reader.GetInt32(0),
                CommonName = reader.GetString(1),
                Species = reader.GetString(2),
                WingSpan = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                EndangeredStatus = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Color = reader.IsDBNull(5) ? "" : reader.GetString(5)
            };
            LocationName = reader.IsDBNull(6) ? "Kayıt Yok" : reader.GetString(6);
            WeatherType = reader.IsDBNull(7) ? "-" : reader.GetString(7);
            ObserverName = reader.IsDBNull(8) ? "-" : reader.GetString(8);

            // ✨ Notu 9. indisten okuyoruz şekerim:
            Notes = reader.IsDBNull(9) ? "Bu gözlem için özel bir not bırakılmamış." : reader.GetString(9);
        }
    }
}