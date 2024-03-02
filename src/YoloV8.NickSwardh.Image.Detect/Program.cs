//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/NickSwardh/YoloDotNet
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using YoloDotNet;
using YoloDotNet.Extensions;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace devMobile.IoT.YoloV8.NickSwardh.Image.Detect
{
   internal class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static void Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloV8.NickSwardh.Image starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", false, true)
               .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load start : {_applicationSettings.ModelPath}");

            using (var yolo = new Yolo(_applicationSettings.ModelPath, false))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done : {_applicationSettings.ModelPath}");
               Console.WriteLine();

               using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(_applicationSettings.ImageInputPath))
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start : {_applicationSettings.ModelPath}");
                  var results = yolo.RunObjectDetection(image);
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done : {_applicationSettings.ModelPath}");

                  Console.WriteLine();

                  foreach (var result in results)
                  {
                     Console.WriteLine($"  Class {result.Label.Name} {(result.Confidence * 100.0):f1}% X:{result.BoundingBox.Left} Y:{result.BoundingBox.Y} Width:{result.BoundingBox.Width} Height:{result.BoundingBox.Height}");
                  }
                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

                  image.Draw(results);

                  image.Save(_applicationSettings.ImageOutputPath);
               }
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
