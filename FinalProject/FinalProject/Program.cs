using Microsoft.EntityFrameworkCore;
using FinalProject.Models; // Asegúrate de tener este using

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración del DbContext (Esto es lo que ya tienes)
builder.Services.AddDbContext<BancoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BancoConnection")));

// 2. Configuración de controladores y vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ... El resto de tu código (app.UseStaticFiles(), etc.)