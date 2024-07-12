//---------------------------------------------------------------------------------
// Copyright (c) April 2024 devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
//
// https://github.com/dotnet/MQTTnet
//---------------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;
using Microsoft.ML.Data;

using FasterrCNNResnet50_Detect_Image;


namespace devMobile.FasterrCNNResnet50.Detect.Image
{
   class Program
   {
      static async Task Main()
      {
         Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} FasterrCNNResnet50 client starting");

         try
         {
            // load the app settings into configuration
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", false, true)
            .Build();

            Model.ApplicationSettings _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

            // Create single instance of sample data from first line of dataset for model input
            var image = MLImage.CreateFromFile(_applicationSettings.ImageInputPath);

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} FasterrCNNResnet50 Model detect start");

            AzureObjectDetection.ModelInput inputData = new AzureObjectDetection.ModelInput()
            {
               ImageSource = image,
            };

            // Make a single prediction on the sample data and print results.
            var predictions = AzureObjectDetection.Predict(inputData);

            Console.WriteLine($" {DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} FasterrCNNResnet50 Model detect done");
            Console.WriteLine();

            Console.WriteLine($" Boxes: {predictions.BoundingBoxes.Length}");

            foreach (var prediction in predictions.BoundingBoxes)
            {
               Console.WriteLine($"  Class:{prediction.Label} {(prediction.Score * 100.0):f1}% X:{prediction.Left:f0} Y:{prediction.Right:f0} Bottom:{prediction.Bottom:f0} Top:{prediction.Top:f0}");
            }
            Console.WriteLine();
         }
         catch (Exception ex)
         {
            Console.WriteLine($"{DateTime.UtcNow:yy-MM-dd HH:mm:ss} Application failure {ex.Message}", ex);
         }

         Console.WriteLine("Press ENTER to exit");
         Console.ReadLine();
      }
   }
}

