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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using CertificateManager;
using CertificateManager.Models;


namespace devMobile.IoT.AzureEventGrid.RootCertificate
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
            Console.WriteLine("No ValidTo using using ValidFrom + ValidFor");

            validTo = validFrom.Add(_applicationSettings.ValidFor.Value);
         }

         if (validFrom >= validTo) 
         {
            Console.WriteLine("validTo must be after ValidFrom");
            return;
         }

         Console.WriteLine($"validFrom:{validFrom} ValidTo:{validTo}");

         var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

         var root = createClientServerAuthCerts.NewRootCertificate(
             new DistinguishedName { 
                     CommonName = _applicationSettings.CommonName,
                     Organisation = _applicationSettings.Organisation,
                     OrganisationUnit = _applicationSettings.OrganisationUnit,
                     Locality = _applicationSettings.Locality,
                     StateProvince  = _applicationSettings.StateProvince,
                     Country = _applicationSettings.Country
             },
             new ValidityPeriod { 
               ValidFrom = validFrom,
               ValidTo = validTo,
             },
             _applicationSettings.PathLengthConstraint,
             _applicationSettings.DnsName);
         root.FriendlyName = _applicationSettings.FriendlyName;


         Console.Write("PFX Password:");
         string password = Console.ReadLine();
         if ( String.IsNullOrEmpty(password))
         {
            Console.WriteLine("PFX Password invalid");
            return;
         }

         var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();

         var rootCertInPfxBtyes = importExportCertificate.ExportRootPfx(password, root);
         File.WriteAllBytes(_applicationSettings.CertificateFilePath, rootCertInPfxBtyes);

         Console.WriteLine($"Root certificate file:{_applicationSettings.CertificateFilePath}");
         Console.WriteLine("press enter to exit");
         Console.ReadLine();
      }
   }
}


namespace devMobile.IoT.AzureEventGrid.RootCertificate.Model
{
   internal class ApplicationSettings
   {
      public string CommonName { get; set; }

      public string Organisation { get; set;}

      public string OrganisationUnit { get; set;}

      public string StateProvince { get; set;}

      public string Locality { get; set;}

      public string Country { get; set;}

      public DateTimeOffset? ValidFrom { get; set;}

      public TimeSpan? ValidFor { get; set; }

      public DateTimeOffset? ValidTo { get; set; }

      public int PathLengthConstraint { get; set;}

      public string DnsName { get; set;}

      public string FriendlyName { get; set;}

      public string CertificateFilePath { get; set; }
   }
}