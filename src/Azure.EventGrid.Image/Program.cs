//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace devMobile.IoT.Azure.EventGrid.Image
{
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
               services.Configure<Model.ApplicationSettings>(hostContext.Configuration.GetSection("ApplicationSettings"));
            })
            .ConfigureLogging(logging =>
            {
               logging.ClearProviders();
               logging.AddSimpleConsole(c => c.TimestampFormat = "[HH:mm:ss.ff]");
            })
            .UseSystemd()
              .ConfigureServices((hostContext, services) =>
              {
                 services.AddHostedService<Worker>();
              });
   }
}
