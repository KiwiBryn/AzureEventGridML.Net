//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
namespace devMobile.IoT.AzureEventGrid.ObjectDetectionImage
{
   using System;

	using System.Net;
	using System.Net.Http;
   using System.Threading;
   using System.Threading.Tasks;

   using Compunet.YoloV8;

   using Microsoft.Extensions.Hosting;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
 
   
   public class Worker : BackgroundService
   {
      private readonly ILogger<Worker> _logger;
      private readonly ApplicationSettings _applicationSettings;
		private HttpClient _httpClient;
      private bool _ImageProcessing = false;
      private Timer _ImageUpdatetimer;
      YoloV8 _predictor;

      public Worker(ILogger<Worker> logger, IOptions<ApplicationSettings> applicationSettings)
      {
         _logger = logger;

         _applicationSettings = applicationSettings.Value;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
         _logger.LogInformation("Azure IoT Smart Edge Camera Service starting");

         try
         {
				NetworkCredential networkCredential = new NetworkCredential(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword);

				_httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = networkCredential });

            using (_predictor = new YoloV8(_applicationSettings.ModelPath))
            {
               _logger.LogInformation($"Timer Due:{_applicationSettings.ImageTimerDue} Period:{_applicationSettings.ImageTimerPeriod}", _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

               _ImageUpdatetimer = new Timer(ImageUpdateTimerCallback, null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);
               try
               {
                  await Task.Delay(Timeout.Infinite, stoppingToken);
               }
               catch (TaskCanceledException)
               {
                  _logger.LogInformation("Application shutown requested");
               }
            }
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Application startup failure");
         }

         _logger.LogInformation("Azure IoT Smart Edge Camera Service shutdown");
      }

      private async void ImageUpdateTimerCallback(object state)
      {
         DateTime requestAtUtc = DateTime.UtcNow;

         // Just incase - stop code being called while photo already in progress
         if (_ImageProcessing)
         {
            return;
         }
         _ImageProcessing = true;

         try
         {
            using (Stream cameraStream = await _httpClient.GetStreamAsync(_applicationSettings.CameraUrl))
            using (Stream fileStream = File.Create(_applicationSettings.ImageCameraFilepath))
            {
               await cameraStream.CopyToAsync(fileStream);
            }

            var result = await _predictor.DetectAsync(_applicationSettings.ImageCameraFilepath);

            Console.WriteLine($"Speed: {result.Speed}");

            foreach (var box in result.Boxes)
            {
               _logger.LogInformation("Class {box.Class} {(box.Confidence * 100.0):f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}", box.Class, box.Confidence, box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
            }
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Camera image download, processing, or telemetry failed");
         }
         finally
         {
            _ImageProcessing = false;
         }

         TimeSpan duration = DateTime.UtcNow - requestAtUtc;

         _logger.LogInformation("Camera Image download, processing and telemetry done {TotalSeconds:f2} sec", duration.TotalSeconds);
      }
   }
}
