//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/sstainba/Yolov8.Net
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

using Yolov8Net;


namespace devMobile.IoT.YoloV8.sstainba.Image.Detect
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
            .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load start : {_applicationSettings.ModelPath}");

            using (var image = SixLabors.ImageSharp.Image.Load(_applicationSettings.ImageInputPath))
            using (var predictor = YoloV8Predictor.Create(_applicationSettings.ModelPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load done : {_applicationSettings.ModelPath}");
               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect start : {_applicationSettings.ModelPath}");
               var predictions = predictor.Predict(image);
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model detect done : {_applicationSettings.ModelPath}");
               Console.WriteLine();

               foreach (var prediction in predictions)
               {
                  Console.WriteLine($"  Class {prediction.Label.Name} {(prediction.Score * 100.0):f1}% X:{prediction.Rectangle.X} Y:{prediction.Rectangle.Y} Width:{prediction.Rectangle.Width} Height:{prediction.Rectangle.Height}");
               }

               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save start : {_applicationSettings.ImageOutputPath}");

               using (SixLabors.ImageSharp.Image output = SixLabors.ImageSharp.Image.Load(_applicationSettings.ImageInputPath))
               {
                  SixLabors.Fonts.Font font = new SixLabors.Fonts.Font(SystemFonts.Get("Arial"), 10);

                  foreach (var pred in predictions)
                  {
                     var originalImageHeight = output.Height;
                     var originalImageWidth = output.Width;

                     var x = (int)Math.Max(pred.Rectangle.X, 0);
                     var y = (int)Math.Max(pred.Rectangle.Y, 0);
                     var width = (int)Math.Min(originalImageWidth - x, pred.Rectangle.Width);
                     var height = (int)Math.Min(originalImageHeight - y, pred.Rectangle.Height);

                     //Note that the output is already scaled to the original image height and width.

                     // Bounding Box Text
                     string text = $"{pred.Label.Name} [{pred.Score}]";
                     var size = TextMeasurer.MeasureSize(text, new TextOptions(font));

                     output.Mutate(d => d.Draw(Pens.Solid(Color.Yellow, 2),
                         new Rectangle(x, y, width, height)));

                     output.Mutate(d => d.DrawText(text, font, Color.Yellow, new Point(x, (int)(y - size.Height - 1))));
                  }

                  output.SaveAsJpeg(_applicationSettings.ImageOutputPath);
               }
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save done : {_applicationSettings.ImageOutputPath}");
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
