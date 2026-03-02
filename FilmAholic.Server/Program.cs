using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<FilmAholicDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    ));

builder.Services.AddIdentity<Utilizador, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<FilmAholicDbContext>()
.AddDefaultTokenProviders();

var authBuilder = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
        options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
        options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
    });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.SaveTokens = true;
        options.Scope.Add("public_profile");
        options.Scope.Add("email");
        options.Fields.Add("name");
        options.Fields.Add("email");
        options.Fields.Add("first_name");
        options.Fields.Add("last_name");
    });
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.None; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
});

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

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IPreferenciasService, PreferenciasService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IMovieService, MovieService>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200", "https://localhost:50905")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

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

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FilmAholicDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            var generosExist = false;
            try
            {
                generosExist = await context.Generos.AnyAsync();
            }
            catch (Exception)
            {
                logger.LogWarning("A tabela Generos ainda não existe. Por favor, aplique as migrações primeiro.");
            }

            if (!generosExist)
            {
                var generosIniciais = new List<Genero>
                {
                    new Genero { Nome = "Ação" },
                    new Genero { Nome = "Aventura" },
                    new Genero { Nome = "Comédia" },
                    new Genero { Nome = "Crime" },
                    new Genero { Nome = "Drama" },
                    new Genero { Nome = "Fantasia" },
                    new Genero { Nome = "Ficção Científica" },
                    new Genero { Nome = "Horror" },
                    new Genero { Nome = "Mistério" },
                    new Genero { Nome = "Romance" },
                    new Genero { Nome = "Thriller" },
                    new Genero { Nome = "Animação" },
                    new Genero { Nome = "Documentário" },
                    new Genero { Nome = "Família" },
                    new Genero { Nome = "Guerra" },
                    new Genero { Nome = "Western" }
                };
                
                context.Generos.AddRange(generosIniciais);
                await context.SaveChangesAsync();
                logger.LogInformation("Géneros iniciais criados com sucesso.");
            }

            var desafiosExist = false;
            try
            {
                desafiosExist = await context.Desafios.AnyAsync();
            }
            catch (Exception)
            {
                logger.LogWarning("A tabela Desafios ainda não existe. Por favor, aplique as migrações primeiro.");
            }

            if (!desafiosExist)
            {
                var seedList = DesafioSeed.Desafios.Select(d => new Desafio
                {
                    DataInicio = d.DataInicio,
                    DataFim = d.DataFim,
                    Descricao = d.Descricao,
                    Ativo = d.Ativo,
                    Genero = d.Genero,
                    QuantidadeNecessaria = d.QuantidadeNecessaria,
                    Xp = d.Xp
                }).ToList();

                context.Desafios.AddRange(seedList);
                await context.SaveChangesAsync();
                logger.LogInformation("Desafios seed inseridos com sucesso.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao popular dados iniciais. Certifique-se de que as migrações foram aplicadas.");
    }
}

app.Run();
