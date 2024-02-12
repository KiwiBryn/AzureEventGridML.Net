//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/dotnet/MQTTnet
//---------------------------------------------------------------------------------
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;


namespace devMobile.IoT.AzureEventGrid.MqttNetClient
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static MQTTnet.Client.IMqttClient _client;
      private static bool _publisherBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} MQTTNet client starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            var mqttFactory = new MqttFactory();

            using (_client = mqttFactory.CreateMqttClient())
            {
               var mqttClientOptions = new MqttClientOptionsBuilder()
                     .WithClientId(_applicationSettings.ClientId)
                     .WithTcpServer(_applicationSettings.Host, _applicationSettings.Port)
                     .WithCredentials(_applicationSettings.UserName, _applicationSettings.Password)
                     .WithCleanSession(_applicationSettings.CleanStart)
                     .WithTlsOptions(new MqttClientTlsOptions() { UseTls = true })
                     .Build();

               var connectResult = await _client.ConnectAsync(mqttClientOptions);
               if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               _client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

               foreach (string topic in _applicationSettings.SubscribeTopics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
               {
                  Console.WriteLine($" Subscribing to {topic}");

                  var subscribeResult = await _client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);

                  Console.WriteLine($" Subscribed to:{topic} Result:{subscribeResult.Items.First().ResultCode}");
               }

               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Due:{_applicationSettings.PublicationTimerDue} Period:{_applicationSettings.PublicationTimerPeriod}");

               Timer imageUpdatetimer = new(PublisherTimerCallback, null, _applicationSettings.PublicationTimerDue, _applicationSettings.PublicationTimerPeriod);

               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} press <ctrl^c> to exit");

               try
               {
                  await Task.Delay(Timeout.Infinite);
               }
               catch (TaskCanceledException)
               {
                  Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown requested");
               }

               await _client.DisconnectAsync();
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application startup failure {ex.Message}", ex);
         }
      }

      private static async void PublisherTimerCallback(object? state)
      {
         // Just incase - stop code being called while photo already in progress
         if (_publisherBusy)
         {
            return;
         }
         _publisherBusy = true;

         try
         {
            var payload = JsonSerializer.Serialize(new
            {
               Content = $"{DateTime.UtcNow:yy-MM-dd HH:mm:ss}",
            });

            var message = new MqttApplicationMessageBuilder()
                  .WithTopic(_applicationSettings.PublishTopic)
                  .WithPayload(payload)
                  .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
               .Build();

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.Publish start");

            var resultPublish = await _client.PublishAsync(message);

            Console.WriteLine($"  Published message to topic:{_applicationSettings.PublishTopic} Reason:{resultPublish.ReasonCode}");

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MqttNet.Publish finish");
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} MQTTnet.Publish failed {ex.Message}");
         }
         finally
         {
            _publisherBusy = false;
         }
      }

      private static Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
      {
         Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.receive start");
         Console.WriteLine($"  topic={e.ApplicationMessage.Topic} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
         Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.receive finish");

         return Task.CompletedTask;
      }
   }
}