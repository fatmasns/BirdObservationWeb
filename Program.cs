using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. ADIM: Servisleri Kaydediyoruz
builder.Services.AddRazorPages();

// ÞEKERÝM BURASI EKSÝK KALMIÞ: Layout ve Index'te Context kullanabilmek için þart!
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<NpgsqlConnection>(sp =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 2. ADIM: Ara Yazýlýmlar (Middleware) Sýralamasý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Þekerim bu sýra çok kritik: Önce Session, sonra Authorization!
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

app.Run();