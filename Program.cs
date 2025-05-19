using geotagger_backend.Data;
using geotagger_backend.Models;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.IO;
using System.Security.Claims;
using Microsoft.Extensions.FileProviders;
using System;
using geotagger_backend.Middleware;
using System.Globalization;
using geotagger_backend.Helpers;


DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(options =>
{
    options.Host = Environment.GetEnvironmentVariable("SMTP__HOST");
    options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP__PORT"), out var port) ? port : 587;
    options.User = Environment.GetEnvironmentVariable("SMTP__USER");
    options.Pass = Environment.GetEnvironmentVariable("SMTP__PASS");
    options.UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("SMTP__USESSL"), out var ssl) ? ssl : true;
    options.From = Environment.GetEnvironmentVariable("SMTP__FROM");
});

builder.Services.AddTransient<IEmailService, EmailService>();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
          .AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IGuessService, GuessService>();

builder.Services.AddTransient<IEmailService, EmailService>();




// action logging endpoint middleware
builder.Services.AddTransient<ActionLoggingMiddleware>();
/*
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
          .WithOrigins(
            "http://localhost:5173",  //React dev server
            "https://localhost:7056"  //swagger UI origin
          )
          .AllowAnyHeader()
          .AllowAnyMethod()
          //use cookies or other credentials in fetch:
          .AllowCredentials();
    });
});*/

// using Microsoft.AspNetCore.Authentication.Google;

//GOOGLE AUTH
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = Environment.GetEnvironmentVariable("AUTH__GOOGLE__CLIENTID");
        options.ClientSecret = Environment.GetEnvironmentVariable("AUTH__GOOGLE__CLIENTSECRET");
        options.CallbackPath = "/signin-google";
    });
/*
//FACEBOOOK AUTH
builder.Services.AddAuthentication()
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["AUTH__FACEBOOK__APPID"];
        options.AppSecret = builder.Configuration["AUTH__FACEBOOK__APPSECRET"];
        options.CallbackPath = "/signin-facebook"; // default
    });
*/


//configure DB Context with MySQL.
/*
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);*/


//use .env
var dbConnStr = Environment.GetEnvironmentVariable("DB_CONN_STR");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        dbConnStr,
        ServerVersion.AutoDetect(dbConnStr)
    )
);

//configure ASP.NET Core Identity.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

//configure JWT from settings.
/*
var jwtSettingsSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);
*/

var jwtKey = Environment.GetEnvironmentVariable("JWT__KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT__ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT__AUDIENCE");
var jwtExpires = Environment.GetEnvironmentVariable("JWT__EXPIRESINMINUTES");

var key = Encoding.UTF8.GetBytes(jwtKey);

// If you need to provide JwtSettings to DI:
builder.Services.Configure<JwtSettings>(options => {
    options.Key = jwtKey;
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.ExpiresInMinutes = int.TryParse(jwtExpires, out var x) ? x : 60;
});



//configure JWT authentication.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})//updated jwt to pull from .env
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            NameClaimType = "id",
            RoleClaimType = ClaimTypes.Role
        };
    });

    /*
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        
        NameClaimType = "id",                 
        RoleClaimType = ClaimTypes.Role
    };*/


//register custom authentication service
builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.AddScoped<INotificationService, NotificationService>();

//add controllers and endpoints explorer
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

//configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Geotagger API",
        Version = "v1",
        Description = "API endpoints for the Geotagger application."
    });

    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });


    //define the JWT Bearer authentication scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
         {
             new OpenApiSecurityScheme
             {
                 Reference = new OpenApiReference
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 },
                 Scheme = "oauth2",
                 Name = "Bearer",
                 In = ParameterLocation.Header,
             },
             new List<string>()
         }
    });
});

builder.Services.AddHttpClient();
/*
builder.Services
    .AddAuthentication()
    .AddGoogle("Google", opts =>
    {
        opts.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
        opts.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
        opts.Scope.Add("email");
        opts.Scope.Add("profile");
    })
    .AddFacebook("Facebook", opts =>
    {
        opts.AppId = builder.Configuration["Auth:Facebook:AppId"]!;
        opts.AppSecret = builder.Configuration["Auth:Facebook:AppSecret"]!;
        opts.Fields.Add("email");
        opts.Fields.Add("name");
    });
*/


var app = builder.Build();

// 1) Enforce HTTPS + HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Adds Strict-Transport-Security header
    app.UseHsts();
}
app.UseExceptionHandlerMiddleware();
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

//Seed the “Admin” role if it doesnst exist
using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roles.RoleExistsAsync("Admin"))
    {
        await roles.CreateAsync(new IdentityRole("Admin"));
    }

    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var matic = await users.FindByEmailAsync("matic.ozimic@gmail.com");
    if (matic != null && !await users.IsInRoleAsync(matic, "Admin"))
        await users.AddToRoleAsync(matic, "Admin");


}




// 0) Resolve & ensure the folders exist
var webRoot = builder.Environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

var imagesPath = Path.Combine(webRoot, "images");
var avatarsPath = Path.Combine(webRoot, "avatars");

Directory.CreateDirectory(imagesPath);
Directory.CreateDirectory(avatarsPath);

// 1)  /images/*  →  wwwroot/images/*
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images",
    ServeUnknownFileTypes = false,          // refuse .exe, .ps1, …
    OnPrepareResponse = ctx =>          // short-circuit caching
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-store";
    }
});

// 2)  /avatars/*  →  wwwroot/avatars/*
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarsPath),
    RequestPath = "/avatars",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        // avatars may be cached for a day
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=86400";
    }
});




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Geotagger API V1");
    });
}




//NO other `UseStaticFiles()` calls
//NO calls to `UseDirectoryBrowser()`


app.UseRouting();

app.UseCors("AllowAll");

// after app.UseRouting();
app.UseMiddleware<ActionLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


// 3) Security headers
/*
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Prevent MIME-type sniffing
    headers["X-Content-Type-Options"] = "nosniff";

    // Protect against clickjacking
    headers["X-Frame-Options"] = "DENY";

    // Control Referer header
    headers["Referrer-Policy"] = "no-referrer";

    // Disable certain powerful features
    headers["Permissions-Policy"] =
        "geolocation=(), microphone=(), camera=()";

    //A fairly strict CSP; 'self' to be adjusted as needed
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https://localhost:7056; " +
        "frame-ancestors 'none';";

    await next();
});
*/
app.MapControllers();

var smtpHost = builder.Configuration.GetValue<string>("SMTP:Host");
Console.WriteLine($"SMTP HOST = {smtpHost}");


//vall the seeding method after building but before running


app.Run();


















