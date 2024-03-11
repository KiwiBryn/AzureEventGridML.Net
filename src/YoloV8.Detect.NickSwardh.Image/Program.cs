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


namespace devMobile.IoT.YoloV8.Detect.NickSwardh.Image
{
   internal class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main()
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

            using (var predictor = new Yolo(_applicationSettings.ModelPath, false))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done");
               Console.WriteLine();

               using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(_applicationSettings.ImageInputPath))
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start");

                  var predictions = predictor.RunObjectDetection(image);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");
                  Console.WriteLine();

                  foreach (var predicition in predictions)
                  {
                     Console.WriteLine($"  Class {predicition.Label.Name} {(predicition.Confidence * 100.0):f1}% X:{predicition.BoundingBox.Left} Y:{predicition.BoundingBox.Y} Width:{predicition.BoundingBox.Width} Height:{predicition.BoundingBox.Height}");
                  }
                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

                  image.Draw(predictions);

                  await image.SaveAsJpegAsync(_applicationSettings.ImageOutputPath);
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
