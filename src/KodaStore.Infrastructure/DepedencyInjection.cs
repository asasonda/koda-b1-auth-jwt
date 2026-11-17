using KodaStore.Application.Repositories;
using KodaStore.Application.Services;
using KodaStore.Infrastructure.Identity;
using KodaStore.Infrastructure.Persistence;
using KodaStore.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KodaStore.Infrastructure;

public static class DepedencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ecommercedb") ?? throw new InvalidOperationException("Connection  string ecommercedb belum dikonfigurasi");

        services.AddDbContext<AppDbContext>(option =>
        {
            option.UseSqlServer(connectionString)
             .UseSeeding((context, _) =>
             {
                 ProductSeedData.Seed(context);
                 CategorySeedData.Seed(context);
                 OrderSeedData.Seed(context);
             })
             .UseAsyncSeeding(async (context, _, ct) =>
             {
                 await ProductSeedData.SeedAsync(context, ct);
                 await CategorySeedData.SeedAsync(context, ct);
                 await OrderSeedData.SeedAsync(context, ct);
             });
        });

        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IProductQueryRepository, EfProductQueryRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();

        services.AddIdentityCore<AppUser>(option =>
        {
            option.User.RequireUniqueEmail = true;
            option.Password.RequiredLength = 8;
            option.Password.RequireDigit = true;
            option.Password.RequireLowercase = true;
            option.Password.RequireUppercase = true;
            option.Password.RequireNonAlphanumeric = true;
            option.Password.RequiredUniqueChars = 4;
            option.SignIn.RequireConfirmedEmail = false;
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IAuthService, IdentityAuthService>();
        
        return services;
    }
}