//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - Azure Event Grid + YoloV8 for object detection PoC
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU
// Affero General Public License as published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. 
// If not, see <https://www.gnu.org/licenses/>
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Azure.EventGrid.Image.Detect
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

