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
namespace devMobile.IoT.Azure.EventGrid.Image.YoloV8.Pose.Model
{
   internal class ApplicationSettings
	{
		public string DeviceId { get; set; }

		public TimeSpan ImageTimerDue { get; set; }
		public TimeSpan ImageTimerPeriod { get; set; }

      public string UserName { get; set; }

      public string ClientCertificateFileName { get; set; }
      public string ClientCertificatePassword { get; set; }

      public int Port { get; set; }
      public string Host { get; set; }
      public string ClientId { get; set; }
      public bool CleanStart { get; set; }


      public string PublishTopic { get; set; }

      public QualityOfService PublishQualityOfService { get; set; }

      public string CameraUrl { get; set; }
      public string CameraUserName { get; set; }
      public string CameraUserPassword { get; set; }

      public string ModelPath { get; set; }
   }

   public enum PoseMarker
   {
      Nose,
      LeftEye,
      RightEye,
      LeftEar,
      RightEar,
      LeftShoulder,
      RightShoulder,
      LeftElbow,
      RightElbow,
      LeftWrist,
      RightWrist,
      LeftHip,
      RightHip,
      LeftKnee,
      RightKnee,
      LeftAnkle,
      RightAnkle
   }
}
