using System.IdentityModel.Tokens.Jwt;
using KodaStore.Api.Middleware;
using KodaStore.Application.Services;
using KodaStore.Infrastructure;
using KodaStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// builder.Services.AddSingleton<InMemoryProductCatalog>();

// builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
// builder.Services.AddSingleton<IProductService, ProductService>();

// Repository SQL Server
// builder.Services.AddScoped<
//     IProductRepository,
//     SqlProductRepository>();

// EFCORE REPOSITORY test
// builder.Services.AddScoped<IProductRepository, EfProductRepository>();
// builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
// builder.Services.AddScoped<IProductQueryRepository, EfProductQueryRepository>();
// builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOption = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Konfiguration JWT belum tersedia");

if (string.IsNullOrWhiteSpace(jwtOption.Issuer))
{
    throw new InvalidOperationException("Jwt: Issuer wajib diisi");
}
if (string.IsNullOrWhiteSpace(jwtOption.Audience))
{
    throw new InvalidOperationException("Jwt: Audience wajib diisi");
}

if(jwtOption.AccessTokenMinutes is < 1 or > 60)
{
    throw new InvalidOperationException("Jwt: Access token minutes antara 1-60");
}
byte[] signingKeyBytes;
try
{
    signingKeyBytes = Convert.FromBase64String(jwtOption.SigninKeyBase64);
}
catch (FormatException e)
{
    throw new InvalidOperationException("Jwt: SigingKeyBase64 tidak menggunakan Base64 yg valid", e);
}

if (signingKeyBytes.Length < 32)
{
    throw new InvalidOperationException("JWT signing key minimal harus 32 byte");
}

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService,OrderService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();


var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(option =>
{
    option.AddPolicy("ReactClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(option =>
{
    option.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    option.AddConcurrencyLimiter("category-writes", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.QueueLimit = 0;
    });
    option.AddFixedWindowLimiter("LoginLimiter", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(3);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        option.MapInboundClaims = false;
        option.IncludeErrorDetails = false;
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOption.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOption.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseCors("ReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();