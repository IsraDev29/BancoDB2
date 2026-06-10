using Microsoft.EntityFrameworkCore;
using BancoAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ── Cadena de conexión exacta para tu instancia SQL Developer ──
builder.Services.AddDbContext<BancoContext>(op =>
    op.UseSqlServer(
        "Server=127.0.0.1,1433;Initial Catalog=BANCO_BIGDATA;User Id=sa;Password=SqlServer2025@;TrustServerCertificate=True;",
        sqlOptions => sqlOptions.CommandTimeout(60)
    )
);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();