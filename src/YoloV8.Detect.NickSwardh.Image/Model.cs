//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.YoloV8.NickSwardh.Image.Detect.Model
{
	public class ApplicationSettings
	{
		public string ImageInputPath{ get; set; }

		public string ImageOutputPath { get; set; }

		public string ModelPath { get; set; }

      public bool Cuda { get; set; }

      public int gpuId { get; set; }

      public double Threshold { get; set; }

   }
}
