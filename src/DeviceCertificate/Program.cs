//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Inspired by https://github.com/damienbod/AspNetCoreCertificates
//
// Thankyou Damien Bod https://damienbod.com/ your blog posts and github were incredibly helpful
//
//---------------------------------------------------------------------------------
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using CertificateManager;
using CertificateManager.Models;


namespace devMobile.IoT.AzureEventGrid.DeviceCertificate
{
   internal class Program
   {
      private static Model.ApplicationSettings _applicationSettings;

      static void Main(string[] args)
      {
         var serviceProvider = new ServiceCollection()
               .AddCertificateManager()
               .BuildServiceProvider();

         // load the app settings into configuration
         var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", false, true)
               .AddUserSecrets<Program>()
         .Build();

         _applicationSettings = configuration.GetSection("ApplicationSettings").Get<Model.ApplicationSettings>();

         DateTimeOffset validFrom = DateTimeOffset.UtcNow.Date;

         if (_applicationSettings.ValidFrom is not null)
         {
            validFrom = _applicationSettings.ValidFrom.Value;
         }
         else
         {
            Console.WriteLine("No ValidFrom using UTC now");
         }

         DateTimeOffset validTo = DateTimeOffset.UtcNow.Date;

         if (!((_applicationSettings.ValidTo is null) ^ (_applicationSettings.ValidFor is null)))
         {
            Console.WriteLine("Must have ValidTo or ValidFor");
            return;
         }

         if (_applicationSettings.ValidTo is not null)
         {
            validTo = _applicationSettings.ValidTo.Value;
         }

         if (_applicationSettings.ValidFor is not null)
         {
            Console.WriteLine("No ValidTo using ValidFrom + ValidFor");

            validTo = validFrom.Add(_applicationSettings.ValidFor.Value);
         }

         if (validFrom >= validTo)
         {
            Console.WriteLine("validTo must be after ValidFrom");
            return;
         }

         Console.WriteLine($"validFrom:{validFrom} ValidTo:{validTo}");

         Console.WriteLine($"Intermediate PFX file:{_applicationSettings.IntermediateCertificateFilePath}");

         Console.Write("Intermediate PFX Password:");
         string intermediatePassword = Console.ReadLine();
         if (String.IsNullOrEmpty(intermediatePassword))
         {
            Console.WriteLine("Intermediate PFX Password invalid");
            return;
         }
         var intermediate = new X509Certificate2(_applicationSettings.IntermediateCertificateFilePath, intermediatePassword);

         Console.Write("Device ID:");
         string deviceId = Console.ReadLine();
         if (String.IsNullOrEmpty(deviceId))
         {
            Console.WriteLine("Device ID invalid");
            return;
         }

         var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

         var device = createClientServerAuthCerts.NewDeviceChainedCertificate(
               new DistinguishedName
               {
                  CommonName = deviceId,
                  Organisation = _applicationSettings.Organisation,
                  OrganisationUnit = _applicationSettings.OrganisationUnit,
                  Locality = _applicationSettings.Locality,
                  StateProvince = _applicationSettings.StateProvince,
                  Country = _applicationSettings.Country
               },
            new ValidityPeriod
            {
               ValidFrom = validFrom,
               ValidTo = validTo,
            },
            deviceId, intermediate);
         device.FriendlyName = deviceId;

         Console.Write("Device PFX Password:");
         string devicePassword = Console.ReadLine();
         if (String.IsNullOrEmpty(devicePassword))
         {
            Console.WriteLine("Fail");
            return;
         }

         var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();

         string devicePfxPath = string.Format(_applicationSettings.DeviceCertificatePfxFilePath, deviceId);

         Console.WriteLine($"Device PFX file:{devicePfxPath}");
         var deviceCertificatePath = importExportCertificate.ExportChainedCertificatePfx(devicePassword, device, intermediate);
         File.WriteAllBytes(devicePfxPath,  deviceCertificatePath);

         Console.WriteLine("press enter to exit");
         Console.ReadLine();
      }
   }
}


namespace devMobile.IoT.AzureEventGrid.DeviceCertificate.Model
{
   internal class ApplicationSettings
   {
      public string Organisation { get; set; }

      public string OrganisationUnit { get; set; }

      public string StateProvince { get; set; }

      public string Locality { get; set; }

      public string Country { get; set; }

      public DateTimeOffset? ValidFrom { get; set; }

      public TimeSpan? ValidFor { get; set; }

      public DateTimeOffset? ValidTo { get; set; }

      public string IntermediateCertificateFilePath { get; set; }

      public string DeviceCertificatePfxFilePath { get; set; }
   }
}