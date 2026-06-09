using Microsoft.EntityFrameworkCore;
using BancoAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ── Cadena de conexión exacta para tu instancia SQL Developer ──
builder.Services.AddDbContext<BancoContext>(op =>
    op.UseSqlServer(
        "Server=.\\SQLDEVELOPER;Database=BANCO_BIGDATA;Trusted_Connection=True;Encrypt=False;",
        sqlOptions => sqlOptions.CommandTimeout(60)
    )
);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();