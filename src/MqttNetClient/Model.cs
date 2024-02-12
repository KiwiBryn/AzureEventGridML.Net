//---------------------------------------------------------------------------------
// Copyright (c) February 2024 devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
//
// https://github.com/dotnet/MQTTnet
//---------------------------------------------------------------------------------
using MQTTnet.Protocol;

namespace devMobile.IoT.AzureEventGrid.MqttNetClient.Model
{
   internal class ApplicationSettings
   {

      public TimeSpan PublicationTimerDue { get; set; }
      public TimeSpan PublicationTimerPeriod { get; set; }

      public string ClientId { get; set; }
      public string Host { get; set; }
      public int Port { get; set; }
      public bool CleanStart { get; set; }

      public string PublishTopic { get; set; }
      public MqttQualityOfServiceLevel PublishQualityOfService { get; set; }

      public string SubscribeTopics { get; set; }
      public MqttQualityOfServiceLevel SubscribeQualityOfService { get; set; }

      public string UserName { get; set; }
      public string Password { get; set; }

   }
}


