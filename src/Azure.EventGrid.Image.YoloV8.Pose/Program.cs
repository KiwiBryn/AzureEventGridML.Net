//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Azure.EventGrid.Image.YoloV8.Pose
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