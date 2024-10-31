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
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


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

                  List<CentroidPoint> centroids = [];

                  foreach (var prediction in predictions.Boxes)
                  {
                     Console.WriteLine($"  Class {prediction.Class} {(prediction.Confidence * 100.0):f1}% X:{prediction.Bounds.X} Y:{prediction.Bounds.Y} Width:{prediction.Bounds.Width} Height:{prediction.Bounds.Height}");

                     centroids.Add(new CentroidPoint(prediction.Bounds));
                  }
                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot detections: {_applicationSettings.ImageOutputPath}");

                  SixLabors.ImageSharp.Image imageOutput = await predictions.PlotImageAsync(image);

                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Calculate and plot clusters  Epsilon:{_applicationSettings.Epsilon} MinimumPointsPerCluster:{_applicationSettings.MinimumPointsPerCluster}");

                  var clusters = Dbscan.Dbscan.CalculateClusters(centroids, _applicationSettings.Epsilon, _applicationSettings.MinimumPointsPerCluster);

                  int colour = 0;
                  foreach (var cluster in clusters.Clusters)
                  {
                     foreach (var clusterPoint in cluster.Objects)
                     {
                        imageOutput.Mutate(d => d.Draw(Pens.Solid(GetColour(colour), 20), new Rectangle((int)clusterPoint.Point.X, (int)clusterPoint.Point.Y, 20, 20)));
                     }
                     colour += 1;
                  }
                  Console.WriteLine();

                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Save image : {_applicationSettings.ImageOutputPath}");

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

      private static Color GetColour(int index)
      {
         return Color.ParseHex(ColourRgb[index % ColourRgb.Length]);
      }

      private static readonly string[] ColourRgb =
            {
            "FF3838",
            "FF9D97",
            "FF701F",
            "FFB21D",
            "CFD231",
            "48F90A",
            "92CC17",
            "3DDB86",
            "1A9334",
            "00D4BB",
            "2C99A8",
            "00C2FF",
            "344593",
            "6473FF",
            "0018EC",
            "8438FF",
            "520085",
            "CB38FF",
            "FF95C8",
            "FF37C7",
         };

   }

   internal class CentroidPoint(Rectangle rectangle) : Dbscan.IPointData
   {
      public Dbscan.Point Point { get; } = new Dbscan.Point(rectangle.X + (rectangle.Width / 2.0), rectangle.Y + (rectangle.Height / 2.0));
   }
}
