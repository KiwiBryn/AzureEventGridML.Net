//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/dme-compunet/YOLOv8
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using Compunet.YoloV8;
using Compunet.YoloV8.Plotting;

using SixLabors.ImageSharp;


namespace devMobile.IoT.YoloV8.dem_compunet.Image.Detect
{
   internal class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloV8.dem-compunetImage starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load start : {_applicationSettings.ModelPath}");

            using (Compunet.YoloV8.YoloV8 predictor = new Compunet.YoloV8.YoloV8(_applicationSettings.ModelPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done : {_applicationSettings.ModelPath}");
               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start : {_applicationSettings.ModelPath}");
               var result = await predictor.DetectAsync(_applicationSettings.ImageInputPath);
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done : {_applicationSettings.ModelPath}");
               Console.WriteLine();

               Console.WriteLine($" Speed: {result.Speed}");

               foreach (var box in result.Boxes)
               {
                  Console.WriteLine($"  Class {box.Class} {(box.Confidence * 100.0):f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}");
               }

               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save start : {_applicationSettings.ImageOutputPath}");

               using var origin = SixLabors.ImageSharp.Image.Load(_applicationSettings.ImageInputPath);
               using (var ploted = await result.PlotImageAsync(origin))
               {
                  ploted.Save(_applicationSettings.ImageOutputPath);
               }
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save done : {_applicationSettings.ImageOutputPath}");
               Console.WriteLine();
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application failure {ex.Message}", ex);
         }

         Console.WriteLine("Press enter to exit");
         Console.ReadLine();
      }
   }
}
