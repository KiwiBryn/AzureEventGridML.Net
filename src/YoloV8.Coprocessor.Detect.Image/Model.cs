﻿//---------------------------------------------------------------------------------
// Copyright (c) May 2024, devMobile Software - YoloV8 + image file Pose PoC
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
using SixLabors.ImageSharp.Processing;

namespace devMobile.IoT.YoloV8.Coprocessor.Detect.Image.Model
{
   public class ApplicationSettings
   {
      public string ModelPath { get; set; }

      public string ImageInputPath { get; set; }

      public string ImageOutputPath { get; set; }

      public string ImageProprocessedPath { get; set; }

      public int IterationsWarmUp { get; set; }

      public int Iterations { get; set; }

      public bool Diagnostics { get; set; }

      public bool UseCuda {  get ; set; }

      public bool UseTensorrt { get; set; }

      public bool UseRocm { get; set; }

      public bool UseTvm { get; set; }

      public string TvmSettings { get; set; }

      public int DeviceId { get; set; }

      public bool InputImageResize { get; set; }

      public ResizeMode InputImageResizeMode { get; set; }

      public int InputImageResizeWidth { get; set; }

      public int InputImageResizeHeight { get; set; }
   }
}
