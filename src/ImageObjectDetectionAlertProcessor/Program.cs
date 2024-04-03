using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
       services.AddApplicationInsightsTelemetryWorkerService();
       services.ConfigureFunctionsApplicationInsights();
#if DEBUG
       services.Configure<LoggerFilterOptions>(options =>
       {
          // Application Insights was only showing Warning, Error & Critical. After following around lots of
          // SO posts figured I would go back to the "source".
          //
          // https://github.com/Azure/azure-functions-dotnet-worker/blob/main/samples/FunctionApp/Program.cs
          //
          LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
          if (toRemove is not null)
          {
             options.Rules.Remove(toRemove);
          }
       });
#endif
    })
    .Build();

host.Run();
