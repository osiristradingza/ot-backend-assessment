using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OT.Assessment.Shared.Data;
using OT.Assessment.Shared.Repositories;
using OT.Assessment.Shared.Services;
using OT.Assessment.Consumer.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    })
    .ConfigureServices((context, services) =>
    {
        // Configure Entity Framework
        services.AddDbContext<CasinoDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

        // Register repositories and services
        services.AddScoped<ICasinoRepository, CasinoRepository>();
        services.AddScoped<ICasinoService, CasinoService>();
        services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
        
        // Register the background service
        services.AddHostedService<CasinoWagerConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Casino Wager Consumer started {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

await host.RunAsync();

logger.LogInformation("Casino Wager Consumer ended {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);