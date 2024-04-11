//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - Azure Event Grid + YoloV8 file PoC
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
using Microsoft.Extensions.Configuration;

using Compunet.YoloV8;
using Compunet.YoloV8.Plotting;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace devMobile.IoT.YoloV8.Detect.dem_compunet.Image
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

            using (var predictor = YoloV8Predictor.Create(_applicationSettings.ModelPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done");
               Console.WriteLine();

               using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(_applicationSettings.ImageInputPath))
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start");

                  var predictions = await predictor.DetectAsync(image);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");
                  Console.WriteLine();

                  Console.WriteLine($" Speed: {predictions.Speed}");

                  foreach (var box in predictions.Boxes)
                  {
                     Console.WriteLine($"  Class {box.Class} {(box.Confidence * 100.0):f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}");
                  }
                  Console.WriteLine($" Items:{predictions.Boxes.Length}");
                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

                  SixLabors.ImageSharp.Image imageOutput = await predictions.PlotImageAsync(image);

                  await imageOutput.SaveAsJpegAsync(_applicationSettings.ImageOutputPath);
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
