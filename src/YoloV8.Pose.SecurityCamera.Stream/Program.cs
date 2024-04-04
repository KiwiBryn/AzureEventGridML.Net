//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - YoloV8 + image file Pose PoC
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
using System.Net;

using Microsoft.Extensions.Configuration;

using Compunet.YoloV8;
using Compunet.YoloV8.Data;
using Compunet.YoloV8.Plotting;

using SixLabors.ImageSharp;


namespace devMobile.IoT.YoloV8.Pose.SecurityCamera.Stream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static bool _cameraBusy = false;
      private static HttpClient _httpClient;
      private static YoloV8Predictor _predictor;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} PoseSecurityCameraStream starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            NetworkCredential networkCredential = new NetworkCredential(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword);

            using (_httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = networkCredential }))
            using (_predictor = YoloV8Predictor.Create(_applicationSettings.ModelPath))
            {
               Timer imageUpdatetimer = new Timer(ImageUpdateTimerCallback, null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

               Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss} press <ctrl^c> to exit");

               try
               {
                  await Task.Delay(Timeout.Infinite);
               }
               catch (TaskCanceledException)
               {
                  Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown requested");
               }
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown failure {ex.Message}", ex);
         }
      }

      private static async void ImageUpdateTimerCallback(object state)
      {
         // Just incase - stop code being called while photo already in progress
         if (_cameraBusy)
         {
            return;
         }
         _cameraBusy = true;

         try
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} YoloV8 Image processing start");

            PoseResult result;

            using (var cameraStream = await _httpClient.GetStreamAsync(_applicationSettings.CameraUrl))
            using (var imageInput = await Image.LoadAsync(cameraStream))
            {
               result = await _predictor.PoseAsync(imageInput);

               await imageInput.SaveAsJpegAsync(_applicationSettings.ImageInputPath);

               using (var outputImage = await result.PlotImageAsync(imageInput))
               {
                  await outputImage.SaveAsJpegAsync(_applicationSettings.ImageOutputPath);
               }
            }

            foreach (var box in result.Boxes)
            {
               Console.WriteLine($" Class {box.Class} {(box.Confidence * 100.0):f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}");

               foreach (var Keypoint in box.Keypoints)
               {
                  Console.WriteLine($" Index {Keypoint.Index} {(Keypoint.Confidence * 100.0):f1}% X:{Keypoint.Point.X} Y:{Keypoint.Point.Y}");
               }
            }

            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} YoloV8 Image processing done");
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} YoloV8 Security camera image download or YoloV8 prediction failed {ex.Message}");
         }
         finally
         {
            _cameraBusy = false;
         }
      }
   }
}
