//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Azure.EventGrid.Image.Detect
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
      private readonly Model.ApplicationSettings _applicationSettings;
      private HttpClient _httpClient;
      private HiveMQClient _mqttclient;
      private bool _ImageProcessing = false;
      private YoloV8Predictor _predictor;
      private Timer _imageUpdateTimer;

      public Worker(ILogger<Worker> logger, IOptions<Model.ApplicationSettings> applicationSettings)
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
            _httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = new NetworkCredential(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword)});

            optionsBuilder
               .WithClientId(_applicationSettings.ClientId)
               .WithBroker(_applicationSettings.Host)
               .WithPort(_applicationSettings.Port)
               .WithUserName(_applicationSettings.UserName)
               .WithClientCertificate(_applicationSettings.ClientCertificateFileName, _applicationSettings.ClientCertificatePassword)
               .WithCleanStart(_applicationSettings.CleanStart)
               .WithUseTls(true);

            using (_mqttclient = new HiveMQClient(optionsBuilder.Build()))
            using (_predictor = YoloV8Predictor.Create(_applicationSettings.ModelPath))
            {
               _mqttclient.OnMessageReceived += OnMessageReceived;

               var connectResult = await _mqttclient.ConnectAsync();
               if (connectResult.ReasonCode != ConnAckReasonCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               _logger.LogInformation("Timer Due:{_applicationSettings.ImageTimerDue} Period:{_applicationSettings.ImageTimerPeriod}", _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

               _imageUpdateTimer = new Timer(ImageUpdateTimerCallback, null, _applicationSettings.ImageTimerDue, _applicationSettings.ImageTimerPeriod);

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

      private async void ImageUpdateTimerCallback(object? state)
      {
         DateTime requestAtUtc = DateTime.UtcNow;

         // Just incase - stop code being called while photo or prediction already in progress
         if (_ImageProcessing)
         {
            return;
         }
         _ImageProcessing = true;

         try
         {
            _logger.LogDebug("Camera request start");

            using (Stream cameraStream = await _httpClient.GetStreamAsync(_applicationSettings.CameraUrl))
            using (Stream fileStream = File.Create(_applicationSettings.ImageCameraFilepath))
            {
               await cameraStream.CopyToAsync(fileStream);
            }

            _logger.LogDebug("Camera request done");

            var result = await _predictor.DetectAsync(_applicationSettings.ImageCameraFilepath);

            _logger.LogDebug("Speed Preprocess:{Preprocess} Postprocess:{Postprocess}", result.Speed.Preprocess, result.Speed.Postprocess);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
               foreach (var box in result.Boxes)
               {
                  _logger.LogDebug(" Class {box.Class} {Confidence:f1}% X:{box.Bounds.X} Y:{box.Bounds.Y} Width:{box.Bounds.Width} Height:{box.Bounds.Height}", box.Class, box.Confidence * 100.0, box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
               }
            }

            var message = new MQTT5PublishMessage
            {
               Topic = string.Format(_applicationSettings.PublishTopic, _applicationSettings.UserName),
               Payload = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new
               {
                  result.Boxes,
               })),
               QoS = _applicationSettings.PublishQualityOfService,
            };

            _logger.LogDebug("HiveMQ.Publish start");

            var resultPublish = await _mqttclient.PublishAsync(message);

            _logger.LogDebug("HiveMQ.Publish done");
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

      private void OnMessageReceived(object? sender, HiveMQtt.Client.Events.OnMessageReceivedEventArgs e)
      {
         _logger.LogInformation("OnMessageReceived Topic:{Topic} QoS:{QoS} Payload:{PayloadAsString}", e.PublishMessage.Topic, e.PublishMessage.QoS, e.PublishMessage.PayloadAsString);
      }
   }
}
