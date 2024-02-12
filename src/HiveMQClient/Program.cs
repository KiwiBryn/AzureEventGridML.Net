//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/hivemq/hivemq-mqtt-client-dotnet 
//---------------------------------------------------------------------------------
using HiveMQtt.Client;

using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;


namespace devMobile.IoT.AzureEventGrid.HiveMQClientApplication
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static HiveMQClient _client;
      private static bool _publisherBusy = false;

      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Hive MQ client starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            var options = new HiveMQClientOptions
            {
               ClientId = _applicationSettings.ClientId,
               Host = _applicationSettings.Host,
               Port = _applicationSettings.Port,
               UserName = _applicationSettings.UserName,
               Password = _applicationSettings.Password,
               CleanStart = _applicationSettings.CleanStart,
               UseTLS = true,
            };

            using (_client = new HiveMQClient(options))
            {
               _client.OnMessageReceived += OnMessageReceived;

               var connectResult = await _client.ConnectAsync();
               if (connectResult.ReasonCode != ConnAckReasonCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               foreach (string topic in _applicationSettings.SubscribeTopics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
               {
                  Console.WriteLine($" Subscribing to {topic}");

                  var subscribeResult = await _client.SubscribeAsync(topic, QualityOfService.ExactlyOnceDelivery);

                  Console.WriteLine($" Subscribed to {topic}: {subscribeResult.Subscriptions[0].SubscribeReasonCode}");
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

            var message = new MQTT5PublishMessage
            {
               Topic = _applicationSettings.PublishTopic,
               Payload = Encoding.ASCII.GetBytes(payload),
               QoS = QualityOfService.ExactlyOnceDelivery,
            };

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.Publish start");

            var resultPublish = await _client.PublishAsync(message);

            Console.WriteLine($"  Published message to topic:{_applicationSettings.PublishTopic} Reason:{resultPublish.ReasonCode}");

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.Publish finish");
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} HiveMQ.Publish failed {ex.Message}");
         }
         finally
         {
            _publisherBusy = false;
         }
      }

      private static void OnMessageReceived(object? sender, HiveMQtt.Client.Events.OnMessageReceivedEventArgs e)
      {
         Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.receive start");
         Console.WriteLine($"  topic={e.PublishMessage.Topic} Payload:{e.PublishMessage.PayloadAsString}");
         Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} HiveMQ.receive finish");
      }
   }
}