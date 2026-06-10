using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using UserRoleApp.Data;
using UserRoleApp.Models;
using UserRoleApp.Resources;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using UserRoleApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Database Context
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity Services
builder.Services.AddIdentity<Users, IdentityRole>(o =>
{
    o.Password.RequireDigit = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie Options
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(1);
    o.SlidingExpiration = true;
});

// JWT Authentication Setup
var jwt = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("JWT ERROR: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

// Authorization Policies
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    o.AddPolicy("FacultyOnly", p => p.RequireRole("Faculty"));
    o.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    o.AddPolicy("AdminOrFaculty", p => p.RequireRole("Admin", "Faculty"));
});

// Custom App Services
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<JwtService>();

// Localization Setup
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResources));
    });

// Swagger/API Explorer Setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserRole API",
        Version = "v1",
        Description = "JWT-protected API with Admin, Faculty, Student roles"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (Swagger adds Bearer prefix automatically)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Execute Database Seeder
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seeder.SeedAsync();
}

// Development Environment Setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserRole API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();


var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("de"),
    new CultureInfo("fr")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new CookieRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
};


app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route Controllers and Views
app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();