//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - Azure Event Grid + YoloV8 for object detection PoC
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
namespace devMobile.IoT.Azure.EventGrid.Image.YoloV8.Detect
{
   internal class Worker(ILogger<Worker> logger, IOptions<Model.ApplicationSettings> applicationSettings) : BackgroundService
   {
      private readonly ILogger<Worker> _logger = logger;
      private readonly Model.ApplicationSettings _applicationSettings = applicationSettings.Value;
      private HttpClient _httpClient;
      private HiveMQClient _mqttclient;
      private bool _ImageProcessing = false;
      private YoloV8Predictor _predictor;
      private Timer _imageUpdateTimer;


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
               .WithClientCertificate(_applicationSettings.ClientCertificateFileName, _applicationSettings.ClientCertificatePassword)
               .WithCleanStart(_applicationSettings.CleanStart)
               .WithUseTls(true);

            using (_httpClient = new HttpClient(new HttpClientHandler { PreAuthenticate = true, Credentials = new NetworkCredential(_applicationSettings.CameraUserName, _applicationSettings.CameraUserPassword)}))
            using (_mqttclient = new HiveMQClient(optionsBuilder.Build()))
            using (_predictor = YoloV8Predictor.Create(_applicationSettings.ModelPath))
            {
               _mqttclient.OnMessageReceived += OnMessageReceived;

               var connectResult = await _mqttclient.ConnectAsync();
               if (connectResult.ReasonCode != ConnAckReasonCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               foreach (string topic in _applicationSettings.SubscribeTopics.Split(",", StringSplitOptions.RemoveEmptyEntries))
               {
                  var result = await _mqttclient.SubscribeAsync(string.Format(topic, _applicationSettings.UserName), _applicationSettings.SunbscribeQualityOfService);

                  _logger.LogInformation(" Subscription Topic:{Topic} QoS:{QoS} ReasonCode:{SubscribeReasonCode}", result.Subscriptions[0].TopicFilter.Topic, result.Subscriptions[0].TopicFilter.QoS, result.Subscriptions[0].SubscribeReasonCode);
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

            DetectionResult result;

            using (Stream cameraStream = await _httpClient.GetStreamAsync(_applicationSettings.CameraUrl))
            {
               result = await _predictor.DetectAsync(cameraStream);
            }

            _logger.LogDebug("Speed Preprocess:{Preprocess} Postprocess:{Postprocess}", result.Speed.Preprocess, result.Speed.Postprocess);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
               _logger.LogDebug("Detection results");

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

         _logger.LogDebug("Camera Image download, processing and telemetry done {TotalSeconds:f2} sec", duration.TotalSeconds);
      }

      private void OnMessageReceived(object? sender, HiveMQtt.Client.Events.OnMessageReceivedEventArgs e)
      {
         _logger.LogInformation("OnMessageReceived Topic:{Topic} QoS:{QoS} Payload:{PayloadAsString}", e.PublishMessage.Topic, e.PublishMessage.QoS, e.PublishMessage.PayloadAsString);
      }

   }
}
