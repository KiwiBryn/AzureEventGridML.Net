//---------------------------------------------------------------------------------
// Copyright (c) March 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/nager/Nager.VideoStream
//
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;

using Nager.VideoStream;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Extensions;
using YoloDotNet.Models;


namespace devMobile.IoT.SecurityCamera.Video.Nager.VideoStream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;
      private static Yolo? _yolo;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} SecurityCamera.Video.Nager.VideoStream starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
                 .AddUserSecrets<Program>()
            .Build();

            _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            if (!Directory.Exists(_applicationSettings.ImageFilepathLocal))
            {
               Directory.CreateDirectory(_applicationSettings.ImageFilepathLocal);
            }

#if INPUT_SOURCE_WEB_CAMERA
            var inputSource = new WebcamInputSource(_applicationSettings.WebCameraDeviceName);
#endif

#if INPUT_SOURCE_RTSP_CAMERA
            var inputSource = new StreamInputSource(_applicationSettings.RtspCameraUrl);
#endif

            var cancellationTokenSource = new CancellationTokenSource();

            using (_yolo = new Yolo(new YoloOptions()
            {
               OnnxModel = _applicationSettings.ModelPath,
               //Cuda = _applicationSettings.CUDA,
               //GpuId = _applicationSettings.GPUId,
               //PrimeGpu = _applicationSettings.PrimeGPU,
               Cuda = false,
               ModelType = ModelType.ObjectDetection,
            }))
            {
               File.WriteAllText($"{_applicationSettings.ImageFilepathLocal}\\{DateTime.Now.Ticks}.txt", "Startup");

               _ = Task.Run(async () => await StartStreamProcessingAsync(inputSource, cancellationTokenSource.Token));

               Console.WriteLine("Press any key to stop");
               Console.ReadKey();

               cancellationTokenSource.Cancel();
            }
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutdown failure {ex.Message}", ex);
         }
      }

      private static async Task StartStreamProcessingAsync(InputSource inputSource, CancellationToken cancellationToken = default)
      {
         Console.WriteLine("Start Stream Processing");
         try
         {
            var client = new VideoStreamClient();

            client.NewImageReceived += NewImageReceived;

#if FFMPEG_INFO_DISPLAY
            //client.FFmpegInfoReceived += FFmpegInfoReceived;
#endif
            await client.StartFrameReaderAsync(inputSource, OutputImageFormat.Png, cancellationToken: cancellationToken);

#if FFMPEG_INFO_DISPLAY
            //client.FFmpegInfoReceived -= FFmpegInfoReceived;
#endif
            Console.WriteLine("End Stream Processing");
         }
         catch (Exception exception)
         {
            Console.WriteLine($"{exception}");
         }
      }

      public static int depth = 0;

      private static void NewImageReceived(byte[] imageData)
      {
         //Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} New image received, bytes:{imageData.Length}");

         var start = DateTime.UtcNow;

         Interlocked.Increment(ref depth);
         var results = _yolo.RunObjectDetection(SKImage.FromEncodedData(imageData),0.75);
         Interlocked.Decrement(ref depth);

         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} Depth:{depth} Duration:{(DateTime.UtcNow - start).TotalMilliseconds}mSec");

         foreach (var result in results)
         {
            Console.WriteLine($"Name: {result.Label.Name} Confidence:{result.Confidence} Bounding Box{ result.BoundingBox}");
            /*
               using (var markedUpImage = SKImage.FromEncodedData(imageData).Draw(results, new KeyPointOptions()))
               {
                  markedUpImage.Save($"{_applicationSettings.ImageFilepathLocal}\\{DateTime.UtcNow.Ticks}.jpg", SKEncodedImageFormat.Jpeg, quality: 20);
               }
            }
            */
         }


         //File.WriteAllBytes($"{_applicationSettings.ImageFilepathLocal}\\{DateTime.Now.Ticks}.png", imageData);
      }

#if FFMPEG_INFO_DISPLAY
      private static void FFmpegInfoReceived(string ffmpegStreamInfo)
      {
         Console.WriteLine(ffmpegStreamInfo);
      }
#endif
   }
}

