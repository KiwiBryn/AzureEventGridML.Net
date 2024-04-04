//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - YoloV8 + image file PoC
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

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Yolov8Net;


namespace devMobile.IoT.YoloV8.Detect.sstainba.Image
{
   internal class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloV8.sstainba starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
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

                  var predictions = predictor.Predict(image);

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done");
                  Console.WriteLine();

                  foreach (var prediction in predictions)
                  {
                     Console.WriteLine($"  Class {prediction.Label.Name} {(prediction.Score * 100.0):f1}% X:{prediction.Rectangle.X:f0} Y:{prediction.Rectangle.Y:f0} Width:{prediction.Rectangle.Width:f0} Height:{prediction.Rectangle.Height:f0}");
                  }

                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

                  // This is a bit hacky should be fixed up in future release
                  Font font = new Font(SystemFonts.Get(_applicationSettings.FontName), _applicationSettings.FontSize);
                  foreach (var prediction in predictions)
                  {
                     var x = (int)Math.Max(prediction.Rectangle.X, 0);
                     var y = (int)Math.Max(prediction.Rectangle.Y, 0);
                     var width = (int)Math.Min(image.Width - x, prediction.Rectangle.Width);
                     var height = (int)Math.Min(image.Height - y, prediction.Rectangle.Height);

                     //Note that the output is already scaled to the original image height and width.

                     // Bounding Box Text
                     string text = $"{prediction.Label.Name} [{prediction.Score}]";
                     var size = TextMeasurer.MeasureSize(text, new TextOptions(font));

                     image.Mutate(d => d.Draw(Pens.Solid(Color.Yellow, 2), new Rectangle(x, y, width, height)));

                     image.Mutate(d => d.DrawText(text, font, Color.Yellow, new Point(x, (int)(y - size.Height - 1))));
                  }

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
