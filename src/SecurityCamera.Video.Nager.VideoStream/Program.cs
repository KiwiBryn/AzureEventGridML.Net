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


namespace devMobile.IoT.SecurityCamera.Video.Nager.VideoStream
{
   class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static async Task Main(string[] args)
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} SecurityCameraImage starting");

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

            //var inputSource = new WebcamInputSource("Microsoft® LifeCam HD-3000");
            var inputSource = new StreamInputSource(_applicationSettings.CameraUrl);

            var cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () => await StartStreamProcessingAsync(inputSource, cancellationTokenSource.Token));
            Console.WriteLine("Press any key for stop");
            Console.ReadKey();
            cancellationTokenSource.Cancel();

            Console.WriteLine("Press any key for quit");
            Console.ReadKey();
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application shutown failure {ex.Message}", ex);
         }
      }

      private static async Task StartStreamProcessingAsync(InputSource inputSource, CancellationToken cancellationToken = default)
      {
         Console.WriteLine("Start Stream Processing");
         try
         {
            var client = new VideoStreamClient();
            client.NewImageReceived += NewImageReceived;
            client.FFmpegInfoReceived += FFmpegInfoReceived;

            await client.StartFrameReaderAsync(inputSource, OutputImageFormat.Png, cancellationToken: cancellationToken);

            client.NewImageReceived -= NewImageReceived;
            client.FFmpegInfoReceived -= FFmpegInfoReceived;
            Console.WriteLine("End Stream Processing");
         }
         catch (Exception exception)
         {
            Console.WriteLine($"{exception}");
         }
      }

      private static void FFmpegInfoReceived(string ffmpegStreamInfo)
      {
         //frame=   77 fps=6.4 q=-0.0 size=  467779kB time=00:00:02.56 bitrate=1493004.8kbits/s dup=16 drop=0 speed=0.214x
         Console.WriteLine(ffmpegStreamInfo);
      }

      private static void NewImageReceived(byte[] imageData)
      {
         Console.WriteLine($"New image received, bytes:{imageData.Length}");
         File.WriteAllBytes($@"frames\{DateTime.Now.Ticks}.png", imageData);
      }
   }
}

