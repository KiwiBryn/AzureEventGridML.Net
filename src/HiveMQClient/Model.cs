//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
using HiveMQtt.MQTT5.Types;

namespace devMobile.IoT.AzureEventGrid.HiveMQClientApplication.Model
{
	internal class ApplicationSettings
	{
		public TimeSpan PublicationTimerDue { get; set; }
		public TimeSpan PublicationTimerPeriod { get; set; }

      public string ClientId { get; set; }
      public string Host{ get; set; }
      public int Port { get; set; }
		public bool CleanStart { get; set; }

      public string PublishTopic { get; set; }
      public QualityOfService PublishQualityOfService { get; set; }

      public string SubscribeTopics { get; set; }
      public QualityOfService SubscribeQualityOfService { get; set; }

      public string UserName { get; set; }
      public string Password { get; set; }

      public string ClientCertificateFileName { get; set; }
      public string ClientCertificatePassword { get; set; }
   }
}
