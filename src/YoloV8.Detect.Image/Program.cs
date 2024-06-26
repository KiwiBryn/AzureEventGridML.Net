﻿//---------------------------------------------------------------------------------
// Copyright (c) May 2024, devMobile Software - YoloV8 + image file Detect PoC
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


namespace devMobile.IoT.YoloV8.Detect.Image
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Detect.Image starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} YoloV8 Model load: {_applicationSettings.ModelPath}");

            YoloV8Builder builder = new YoloV8Builder();

            builder.UseOnnxModel(_applicationSettings.ModelPath);

            /*
            builder.WithConfiguration(c =>
            {
            });
            */

            /*
            builder.WithSessionOptions(new Microsoft.ML.OnnxRuntime.SessionOptions()
            {

            });
            */

            using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(_applicationSettings.ImageInputPath))
            using (var predictor = builder.Build())
            {
               var result = await predictor.DetectAsync(image);

               Console.WriteLine();
               Console.WriteLine($"Speed: {result.Speed}");
               Console.WriteLine();

               foreach (var prediction in result.Boxes)
               {
                  Console.WriteLine($" Class {prediction.Class} {(prediction.Confidence * 100.0):f1}% X:{prediction.Bounds.X} Y:{prediction.Bounds.Y} Width:{prediction.Bounds.Width} Height:{prediction.Bounds.Height}");
               }

               Console.WriteLine();

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

               using (var imageOutput = await result.PlotImageAsync(image))
               {
                  await imageOutput.SaveAsJpegAsync(_applicationSettings.ImageOutputPath);
               }
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application failure {ex}");
         }

         Console.WriteLine("Press enter to exit");
         Console.ReadLine();
      }
   }
}
