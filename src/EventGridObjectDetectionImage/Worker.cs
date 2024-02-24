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
   using System.Text.Json;
   using System.Text;
   using System.Threading;
   using System.Threading.Tasks;

   using Microsoft.Extensions.Hosting;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   using Compunet.YoloV8;

   using HiveMQtt.Client;
   using HiveMQtt.MQTT5.ReasonCodes;
   using HiveMQtt.MQTT5.Types;


   public class Worker : BackgroundService
   {
      private readonly ILogger<Worker> _logger;
      private readonly ApplicationSettings _applicationSettings;
      private HttpClient _httpClient;
      private HiveMQClient _Mqttclient;
      private bool _ImageProcessing = false;
      private Timer _imageUpdatetimer;
      private YoloV8 _predictor;

      public Worker(ILogger<Worker> logger, IOptions<ApplicationSettings> applicationSettings)
      {
         _logger = logger;

         _applicationSettings = applicationSettings.Value;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
         _logger.LogInformation("Azure IoT Smart Edge Camera Service starting");

         var optionsBuilder = new HiveMQClientOptionsBuilder();

         try
         {
            optionsBuilder
               .WithClientId(_applicationSettings.ClientId)
               .WithBroker(_applicationSettings.Host)
               .WithPort(_applicationSettings.Port)
               .WithUserName(_applicationSettings.UserName)
               .WithCleanStart(_applicationSettings.CleanStart)
               .WithClientCertificate(_applicationSettings.ClientCertificateFileName, _applicationSettings.ClientCertificatePassword)
               .WithUseTls(true);

            NetworkCredential networkCredential = new NetworkCredential(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword);

            _httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = networkCredential });

            using (_Mqttclient = new HiveMQClient(optionsBuilder.Build()))
            using (_predictor = new YoloV8(_applicationSettings.ModelPath))
            {
               _Mqttclient.OnMessageReceived += OnMessageReceived;

               var connectResult = await _Mqttclient.ConnectAsync();
               if (connectResult.ReasonCode != ConnAckReasonCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               _logger.LogInformation($"Timer Due:{_applicationSettings.ImageTimerDue} Period:{_applicationSettings.ImageTimerPeriod}", _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

               _imageUpdatetimer = new Timer(ImageUpdateTimerCallback, null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

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
               _logger.LogInformation("Class {box.Class} {Confidence:f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}", box.Class, box.Confidence * 100.0, box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
            }

            var payload = JsonSerializer.Serialize(new
            {
               result.Boxes,
            });

            var message = new MQTT5PublishMessage
            {
               Topic = string.Format(_applicationSettings.PublishTopic, _applicationSettings.UserName),
               Payload = Encoding.ASCII.GetBytes(payload),
               QoS = _applicationSettings.PublishQualityOfService,
            };

            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.Publish start");

            var resultPublish = await _Mqttclient.PublishAsync(message);
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

      private static void OnMessageReceived(object? sender, HiveMQtt.Client.Events.OnMessageReceivedEventArgs e)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.receive start");
         Console.WriteLine($" Topic:{e.PublishMessage.Topic} QoS:{e.PublishMessage.QoS} Payload:{e.PublishMessage.PayloadAsString}");
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.receive finish");
      }
   }
}
