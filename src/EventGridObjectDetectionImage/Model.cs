//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.AzureEventGrid.ObjectDetectionImage
{
	using System;

	public class ApplicationSettings
	{
		public string DeviceId { get; set; }

		public TimeSpan ImageTimerDue { get; set; }
		public TimeSpan ImageTimerPeriod { get; set; }

      public string CameraUrl { get; set; }
      public string CameraUserName { get; set; }
      public string CameraUserPassword { get; set; }

      public string ImageCameraFilepath { get; set; }
		public string ImageMarkedUpFilepath { get; set; }

      public string ModelPath { get; set; }

      public string ImageMarkUpFontPath { get; set; }
      public int ImageMarkUpFontSize { get; set; }

		public double PredictionScoreThreshold { get; set; }

#if AZURE_STORAGE_IMAGE_UPLOAD
	public class AzureStorageSettings
	{
		public string ImageCameraFilenameFormat { get; set; }
		public string ImageMarkedUpFilenameFormat { get; set; }
	}
#endif
   }
}
