//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/dotnet/MQTTnet
//---------------------------------------------------------------------------------
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;


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
               MqttClientOptions mqttClientOptions;

               if (string.IsNullOrWhiteSpace(_applicationSettings.ClientCertificateFileName))
               {
                  mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithClientId(_applicationSettings.ClientId)
                        .WithTcpServer(_applicationSettings.Host, _applicationSettings.Port)
                        .WithCredentials(_applicationSettings.UserName, _applicationSettings.Password)
                        .WithCleanStart(_applicationSettings.CleanStart)
                        .WithTlsOptions(new MqttClientTlsOptions(){UseTls = true})
                        .Build();
               }
               else
               {
                  // Certificate based authentication
                  List<X509Certificate2> certificates = new List<X509Certificate2>
                  {
                     new X509Certificate2(_applicationSettings.ClientCertificateFileName, _applicationSettings.ClientCertificatePassword)
                  };

                  var tlsOptions = new MqttClientTlsOptionsBuilder()
                       .WithClientCertificates(certificates)
                       .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                       .UseTls(true)
                       .Build();

                  mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithClientId(_applicationSettings.ClientId)
                        .WithTcpServer(_applicationSettings.Host, _applicationSettings.Port)
                        .WithCredentials(_applicationSettings.UserName, _applicationSettings.Password)
                        .WithCleanStart(_applicationSettings.CleanStart)
                        .WithTlsOptions(tlsOptions)
                        .Build();
               }

               var connectResult = await _client.ConnectAsync(mqttClientOptions);
               if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
               {
                  throw new Exception($"Failed to connect: {connectResult.ReasonString}");
               }

               _client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

               Console.WriteLine($"Subscribed to Topic");
               foreach (string topic in _applicationSettings.SubscribeTopics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
               {
                  var subscribeResult = await _client.SubscribeAsync(topic, _applicationSettings.SubscribeQualityOfService);

                  Console.WriteLine($" {topic} Result:{subscribeResult.Items.First().ResultCode}");
               }

               Console.WriteLine($"Timer Due:{_applicationSettings.PublicationTimerDue} Period:{_applicationSettings.PublicationTimerPeriod}");

               Timer imageUpdatetimer = new(PublisherTimerCallback, null, _applicationSettings.PublicationTimerDue, _applicationSettings.PublicationTimerPeriod);

               Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} press <ctrl^c> to exit");
               Console.WriteLine();

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
         // Just incase - stop code being called while publish already in progress
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
                  .WithTopic(string.Format(_applicationSettings.PublishTopic, _applicationSettings.UserName))
                  .WithPayload(payload)
                  .WithQualityOfServiceLevel(_applicationSettings.PublishQualityOfService)
               .Build();

            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.Publish start");

            var resultPublish = await _client.PublishAsync(message);

            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MqttNet.Publish finish");

            Console.WriteLine($" Published message to Topic:{message.Topic} Reason:{resultPublish.ReasonCode}");
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
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.receive start");
         Console.WriteLine($" Topic:{e.ApplicationMessage.Topic} QoS:{e.ApplicationMessage.QualityOfServiceLevel} Payload:{e.ApplicationMessage.ConvertPayloadToString()}");
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss:fff} MQTTnet.receive finish");

         return Task.CompletedTask;
      }
   }
}