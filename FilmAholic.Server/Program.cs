using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<FilmAholicDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    ));

// Configurar Identity com opções personalizadas
builder.Services.AddIdentity<Utilizador, IdentityRole>(options =>
{
    // Requer confirmação de email (mas não para login externo)
    options.SignIn.RequireConfirmedEmail = true;
    // Configurações de password
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    // Configurações de token
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<FilmAholicDbContext>()
.AddDefaultTokenProviders();

// Configurar autenticação externa (OAuth)
var authBuilder = builder.Services.AddAuthentication();

// Adicionar Google OAuth apenas se as credenciais estiverem configuradas
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true; // Guardar tokens para debug
        // Adicionar scopes explícitos
        options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
        options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
    });
}

// Adicionar Facebook OAuth apenas se as credenciais estiverem configuradas
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.SaveTokens = true; // Guardar tokens para debug
        options.Scope.Add("public_profile");
        options.Scope.Add("email");
        options.Fields.Add("name");
        options.Fields.Add("email");
        options.Fields.Add("first_name");
        options.Fields.Add("last_name");
    });
}

// Configurar cookies para autenticação
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.None; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
});

// Configurar cookies para autenticação externa (OAuth)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10); 
    options.Cookie.Name = ".AspNetCore.ExternalAuth"; 
    options.Cookie.Path = "/"; 
    options.SlidingExpiration = false; 
});

// Registar serviço de email
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("https://localhost:50905")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
