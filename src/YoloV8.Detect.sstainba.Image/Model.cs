//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.YoloV8.sstainba.Image.Detect.Model
{
	public class ApplicationSettings
	{
		public string ImageInputPath{ get; set; }

		public string ImageOutputPath { get; set; }

		public string ModelPath { get; set; }

      public string FontName {  get; set; }

		public int FontSize { get; set; }
   }
}
