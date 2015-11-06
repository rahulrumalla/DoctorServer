using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using Newtonsoft.Json;

namespace DoctorServer
{
    class Program
    {
        private static void Main(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            List<ServerModel> servers = GetServers();

            sb.AppendLine(
                Verify("PING TEST",
                    servers,
                    CanPing,
                    (s, b) => $"{s.Name}:\n - Ping to {s.Address} was " + (b ? "OK" : "FAILED")));

            sb.AppendLine(
                Verify("TCP CONNECTION TEST",
                    servers,
                    CanConnectOverPort,
                    (s, b) => $"{s.Name}:\n - Connection to {s.Address} over port {s.Port} was " + (b ? "OK" : "FAILED")));

            Console.WriteLine(sb.ToString());

            Console.ReadLine();
        }

        private static string Verify(
            string verficationTitle,
            List<ServerModel> servers, 
            Func<ServerModel, bool> verificationCallback,
            Func<ServerModel, bool, string> verificationOutputCallback
            )
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("==============================");
            sb.AppendLine(verficationTitle);
            sb.AppendLine("==============================");

            foreach (var server in servers)
            {
                bool success = verificationCallback(server);

                sb.AppendLine(verificationOutputCallback(server, success));
            }

            return sb.ToString();
        }

        private static List<ServerModel> GetServers()
        {
            var fileContents = File.ReadAllText("servers.json");

            List<ServerModel> servers = JsonConvert.DeserializeObject<List<ServerModel>>(fileContents);

            return servers;
        }

        private static bool CanPing(ServerModel server)
        {
            return CanPing(server.Address);
        }

        private static bool CanPing(string hostName)
        {
            bool success = false;

            try
            {
                var ping = new Ping();

                PingReply pingReply = ping.Send(hostName);

                if (pingReply != null) success = pingReply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return success;
        }

        private static bool CanConnectOverPort(ServerModel serverModel)
        {
            return CanConnectOverPort(serverModel.Address, serverModel.Port);
        }

        private static bool CanConnectOverPort(string hostName, int port)
        {
            bool success = false;

            using (var tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(hostName, port);

                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    tcpClient.Close();
                }
            }

            return success;
        }
        private static bool HasReadPermissionOnDirectory(string path)
        {
            return HasPermissionOnDirectory(path, FileSystemRights.Read);
        }

        private static bool HasWritePermissionOnDirectory(string path)
        {
            return HasPermissionOnDirectory(path, FileSystemRights.Write);
        }

        private static bool HasPermissionOnDirectory(string path, FileSystemRights fileSystemRight)
        {
            bool success = false;

            try
            {
                bool permAllow = false;

                bool permDeny = false;

                DirectorySecurity accessControlList = Directory.GetAccessControl(path);

                if (accessControlList == null)
                    return false;

                var accessRules = accessControlList.GetAccessRules(
                    true,
                    true,
                    typeof (System.Security.Principal.SecurityIdentifier)
                    );

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((fileSystemRight & rule.FileSystemRights) != fileSystemRight)
                        continue;

                    switch (rule.AccessControlType)
                    {
                        case AccessControlType.Allow:
                            permAllow = true;
                            break;

                        case AccessControlType.Deny:
                            permDeny = true;
                            break;
                    }
                }

                success = permAllow && !permDeny;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return success;
        }
    }
}
