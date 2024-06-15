//---------------------------------------------------------------------------------
// Copyright (c) May 2024, devMobile Software - YoloV8 + Coprocessor + image file Detect PoC
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


namespace devMobile.IoT.YoloV8.Coprocessor.Detect.Image
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

            if (_applicationSettings.UseCuda)
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Using CUDA");

               builder.UseCuda(_applicationSettings.DeviceId) ;
            }

            if (_applicationSettings.UseTensorrt)
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Using TensorRT");

               builder.UseTensorrt(_applicationSettings.DeviceId);
            }

            /*            
            builder.WithConfiguration(c =>
            {
               c.Confidence = 0.0f;
               c.IoU = 0.0f;
               c.KeepOriginalAspectRatio = false;
               c.SuppressParallelInference = false ;
            });
            */
            
            /*
            builder.WithSessionOptions(new Microsoft.ML.OnnxRuntime.SessionOptions()
            {
               EnableCpuMemArena
               EnableMemoryPattern
               EnableProfiling = true,
               ExecutionMode = ExecutionMode.
               GraphOptimizationLevel = GraphOptimizationLevel.
               InterOpNumThreads = 1,
               ProfileOutputPathPrefix = ""
               OptimizedModelFilePath = ""                
            });
            */

            using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(_applicationSettings.ImageInputPath))
            {
               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Build start");

               using (var predictor = builder.Build())
               {
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Build done");

                  var result = await predictor.DetectAsync(image);

                  Console.WriteLine();
                  Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Warmup Inference: {result.Speed.Inference.TotalMilliseconds:F0}mSec");
                  Console.WriteLine();

                  TimeSpan duration = new TimeSpan();

                  for (var i = 0; i < _applicationSettings.Iterations; i++)
                  {
                     result = await predictor.DetectAsync(image);

                     duration += result.Speed.Inference;

                     if (_applicationSettings.Diagnostics)
                     {
                        Console.WriteLine($"Boxes:{result.Boxes.Length}");

                        foreach (var prediction in result.Boxes)
                        {
                           Console.WriteLine($" Class {prediction.Class} {(prediction.Confidence * 100.0):F0}% X:{prediction.Bounds.X} Y:{prediction.Bounds.Y} Width:{prediction.Bounds.Width} Height:{prediction.Bounds.Height}");
                        }

                        Console.WriteLine();
                     }
                     else
                     {
                        Console.Write(".");
                     }

                     if (_applicationSettings.Diagnostics)
                     {
                        Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Plot and save : {_applicationSettings.ImageOutputPath}");

                        using (var imageOutput = await result.PlotImageAsync(image))
                        {
                           await imageOutput.SaveAsJpegAsync(_applicationSettings.ImageOutputPath);
                        }
                     }
                  }

                  Console.WriteLine();
                  Console.WriteLine($"Inference duration Average:{duration.TotalMilliseconds / _applicationSettings.Iterations:f0}mSec Iterations:{_applicationSettings.Iterations}");
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
