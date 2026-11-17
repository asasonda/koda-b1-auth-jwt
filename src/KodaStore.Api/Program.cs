using KodaStore.Api.Middleware;
using KodaStore.Application.Services;
using KodaStore.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

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

// Business/Application Service
builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Membaca daftar origin di env.Development
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

// Daftarkan Configuration CORS
builder.Services.AddCors(option =>
{
    option.AddPolicy("ReactClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// DBCONTEXT
// var connectionString = builder.Configuration.GetConnectionString("ecommercedb")
//         ?? throw new InvalidOperationException("ConnectionString : Belum dikonfigurasi");

// builder.Services.AddDbContext<AppDbContext>(options =>
// {
//     options.UseSqlServer(connectionString)
//         .UseSeeding((context, _) =>
//         {
//             ProductSeedData.Seed(context);
//             CategorySeedData.Seed(context);
//             OrderSeedData.Seed(context);
//         })
//         .UseAsyncSeeding(async (context, _, ct) =>
//         //    SeedAllAsync(context, ct)
//             {
//                 await ProductSeedData.SeedAsync(context, ct);
//                 await CategorySeedData.SeedAsync(context, ct);
//                 await OrderSeedData.SeedAsync(context, ct);
//             });
// });

builder.Services.AddRateLimiter(option =>
{
    option.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    option.AddConcurrencyLimiter("category-writes", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.QueueLimit = 0;
    });
});

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
app.UseMiddleware<RequestTimingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ReactClient");
app.UseRouting();

app.UseRateLimiter();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// static async Task SeedAllAsync(DbContext context, CancellationToken ct)
// {
//     await ProductSeedData.SeedAsync(context, ct);
//     await OrderSeedData.SeedAsync(context, ct);
// }
