using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TarkovLootScanner.Services;

namespace TarkovLootScanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var startTime = DateTime.Now;
        base.OnStartup(e);

        try
        {
            // Setup dependency injection
            var diStart = DateTime.Now;
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            var diTime = DateTime.Now - diStart;

            var logger = ServiceProvider.GetRequiredService<ILoggerService>();
            logger.LogInformation("Application startup initiated");
            await logger.LogPerformanceAsync("DependencyInjection", diTime, $"{serviceCollection.Count} services registered");

            // Initialize cache during startup (blocks until first load completes)
            var cacheInitService = ServiceProvider.GetRequiredService<CacheInitializationService>();
            await cacheInitService.InitializeCacheAsync();

            var totalStartupTime = DateTime.Now - startTime;
            await logger.LogPerformanceAsync("Application_Startup", totalStartupTime, "MainWindow ready and cache initialized");
        }
        catch (Exception ex)
        {
            // Fallback logging if DI fails
            var fallbackLogger = new FileLoggerService();
            fallbackLogger.LogError("Critical error during application startup", ex);
            throw;
        }
    }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register core services first
            services.AddSingleton<ILoggerService, FileLoggerService>();

            // Services that depend on logger
            services.AddSingleton<ICacheService, FileCacheService>();
            services.AddSingleton<IPriceCalculationService, TarkovPriceService>();
            services.AddSingleton<ITarkovApiService, TarkovApiService>();

            // Add cache initialization service
            services.AddSingleton<CacheInitializationService>();

            // Register other services that depend on above
            services.AddTransient(typeof(MainWindow));
            services.AddTransient(typeof(UI.OverlayWindow));
        }
    }

}
