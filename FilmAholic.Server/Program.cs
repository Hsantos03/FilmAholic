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
        options.CallbackPath = "/api/autenticacao/google-callback";
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
        options.CallbackPath = "/api/autenticacao/facebook-callback";
        options.SaveTokens = true; // Guardar tokens para debug
        // Configurar scopes (permissões)
        options.Scope.Add("public_profile");
        options.Scope.Add("email");
        // Configurações adicionais para melhorar a compatibilidade
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
    options.Cookie.SameSite = SameSiteMode.None; // Necessário para cross-site (Angular em porta diferente)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sempre HTTPS (requerido com SameSite=None)
});

// Configurar cookies para autenticação externa (OAuth)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; // Necessário para OAuth cross-site
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sempre HTTPS (requerido com SameSite=None)
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10); // Cookies OAuth expiram após 10 minutos
    options.Cookie.Name = ".AspNetCore.ExternalAuth"; // Nome explícito para o cookie
    options.Cookie.Path = "/"; // Garantir que o cookie está disponível em todo o path
    // Configurações adicionais para melhorar a preservação do estado
    options.SlidingExpiration = false; // Não renovar automaticamente para OAuth
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
