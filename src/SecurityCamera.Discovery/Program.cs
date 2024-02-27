//---------------------------------------------------------------------------------
// Copyright (c) February 2023, devMobile Software
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// https://github.com/hivemq/hivemq-mqtt-client-dotnet 
//
// From  https://specs.xmlsoap.org/ws/2005/04/discovery/ws-discovery.pdf
//       http://www.onvif.org/wp-content/uploads/2016/12/ONVIF_WG-APG-Application_Programmers_Guide-1.pdf
//---------------------------------------------------------------------------------
namespace devMobile.IoT.SecurityCameraClient.Discovery
{
   using System;
   using System.Collections.Generic;
   using System.Net;
   using System.Net.NetworkInformation;
   using System.Net.Sockets;
   using System.Text;
   using System.Threading.Tasks;

   class Program
   {
      const string WSDiscoveryProbeMessages =
         "<?xml version = \"1.0\" encoding=\"UTF-8\"?>" +
         "<e:Envelope xmlns:e=\"http://www.w3.org/2003/05/soap-envelope\" " +
            "xmlns:w=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" " +
            "xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" " +
            "xmlns:dn=\"http://www.onvif.org/ver10/network/wsdl\"> " +
               "<e:Header>" +
                  "<w:MessageID>uuid:{0}</w:MessageID>" +
                  "<w:To e:mustUnderstand=\"true\">urn:schemas-xmlsoap-org:ws:2005:04:discovery</w:To> " +
                  "<w:Action mustUnderstand=\"true\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</w:Action> " +
               "</e:Header> " +
               "<e:Body> " +
                  "<d:Probe> " +
                     "<d:Types>dn:NetworkVideoTransmitter</d:Types>" +
                  "</d:Probe> " +
               "</e:Body> " +
         "</e:Envelope>";

      static async Task Main()
      {
         List<UdpClient> udpClients = new List<UdpClient>();

         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
         {
            if (((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) || (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                     && ((networkInterface.Supports(NetworkInterfaceComponent.IPv4) // Not certain if this is necessary but IPV6 could be ++slow
                     && (networkInterface.OperationalStatus == OperationalStatus.Up)))) // If my wifi adaptor wasn't connected...
            {
               Console.WriteLine($"Name:{networkInterface.Name} Type:{networkInterface.NetworkInterfaceType}");

               foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
               {
                  if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                  {
                     var udpClient = new UdpClient(new IPEndPoint(unicastAddress.Address, 0)) { EnableBroadcast = true };

                     udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);

                     udpClients.Add(udpClient);
                  }
               }
            }
         }

         Console.WriteLine();

         var multicastEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 3702);

         foreach (UdpClient udpClient in udpClients)
         {
            byte[] message = UTF8Encoding.UTF8.GetBytes(string.Format(WSDiscoveryProbeMessages, Guid.NewGuid().ToString()));

            try
            {
               Console.WriteLine($"Probing start...");

               await udpClient.SendAsync(message, message.Length, multicastEndpoint);

               IPEndPoint remoteEndPoint = null;

               while (true)
               {
                  message = udpClient.Receive(ref remoteEndPoint);

                  Console.WriteLine($"Probing done...");

                  Console.WriteLine($"IPAddress {remoteEndPoint.Address}");
                  Console.WriteLine(UTF8Encoding.UTF8.GetString(message));
                  Console.WriteLine();
               }
            }
            catch (SocketException sex)
            {
               Console.WriteLine($"Probe failed {sex.Message}");
            }
         }

         Console.WriteLine("Press enter to <exit>");
         Console.ReadLine();
      }
   }
}