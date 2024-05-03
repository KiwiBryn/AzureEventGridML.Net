//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using System;

namespace devMobile.IoT.SecurityCamera.Video.SharpRtsp.VideoStream.Model
{
	public class ApplicationSettings
	{
		public string RtspCameraUrl { get; set; }

      public string WebCameraDeviceName { get; set; }

      public string ImageFilepathLocal { get; set; }
	}
}
