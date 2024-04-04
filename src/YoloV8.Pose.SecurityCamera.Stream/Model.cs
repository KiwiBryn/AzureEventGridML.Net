﻿//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - YoloV8 + image file Pose PoC
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
