using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using BirdObservationWeb.Models;

namespace BirdObservationWeb.Pages
{
    public class AddBirdModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public Bird NewBird { get; set; }

        public AddBirdModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                // pgAdmin'de yazdýðýn prosedürü burada çaðýrýyoruz
                using (var cmd = new NpgsqlCommand("CALL insert_bird(@p1, @p2, @p3, @p4, @p5)", conn))
                {
                    cmd.Parameters.AddWithValue("p1", NewBird.CommonName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p2", NewBird.Species ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p3", NewBird.WingSpan);
                    cmd.Parameters.AddWithValue("p4", NewBird.EndangeredStatus ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p5", NewBird.Color ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            // Kayýt bitince ana sayfaya dön
            return RedirectToPage("Index");
        }
    }
}