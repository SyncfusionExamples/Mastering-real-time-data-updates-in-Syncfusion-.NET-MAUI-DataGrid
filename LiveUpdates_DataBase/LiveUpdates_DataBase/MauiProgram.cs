using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using Firebase.Database;

namespace LiveUpdates_DataBase
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

			builder.Services.AddSingleton<StocksService>();      // Service for Firebase operations 
            builder.Services.AddSingleton<StockViewModel>();     // ViewModel for binding to UI 
            builder.ConfigureSyncfusionCore();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
