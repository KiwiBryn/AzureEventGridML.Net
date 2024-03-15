//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.YoloV8.Pose.SecurityCamera.Stream.Model
{
	public class ApplicationSettings
   {
      public TimeSpan ImageTimerDue { get; set; }
      public TimeSpan ImageTimerPeriod { get; set; }

      public string CameraUrl { get; set; }
      public string CameraUserName { get; set; }
      public string CameraUserPassword { get; set; }

      public string ModelPath { get; set; }

      public string ImageInputPath { get; set; }

      public string ImageOutputPath { get; set; }
   }
}
