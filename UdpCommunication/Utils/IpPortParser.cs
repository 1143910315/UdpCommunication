using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UdpCommunication.Utils
{
    internal class IpPortParser
    {
        private static readonly Regex IpPortRegex = new Regex(
        @"^(?:\[(?<ip>.+)\]|(?<ip>[^:]+)):(?<port>\d+)$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture
    );

        public static IPAddress ParseIp(string input)
        {
            var match = IpPortRegex.Match(input);
            if (!match.Success)
            {
                throw new FormatException("无效的 IP:端口 格式");
            }

            var ip = match.Groups["ip"].Value;

            // 验证 IP 格式
            if (!IPAddress.TryParse(ip, out IPAddress? ipAddress))
            {
                throw new FormatException("无效的 IP 地址格式");
            }

            return ipAddress;
        }

        public static IPEndPoint ParseIpPort(string input)
        {
            var match = IpPortRegex.Match(input);
            if (!match.Success)
            {
                throw new FormatException("无效的 IP:端口 格式");
            }

            var ip = match.Groups["ip"].Value;
            if (!int.TryParse(match.Groups["port"].Value, out int port))
            {
                port = 9720;
            }
            // 验证 IP 格式
            if (!IPAddress.TryParse(ip, out IPAddress? ipAddress))
            {
                throw new FormatException("无效的 IP 地址格式");
            }

            return new IPEndPoint(ipAddress, port);
        }
        public static IPEndPoint? GetEndPointByName(string name)
        {
            string[] parts = name.Split(':');
            if (parts.Length != 2)
            {
                // RainMeadow.Debug("Invalid IP format without colon: " + name);
                parts = new string[2];
                parts[0] = name;
                parts[1] = "8720"; //default port
            }

            IPAddress? address = null;
            try
            {
                address = IPAddress.Parse(parts[0]);
            }
            catch (FormatException)
            {
                try
                {
                    address = Dns.GetHostEntry(parts[0])
                        .AddressList
                        .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                        .FirstOrDefault();
                }
                catch (SocketException)
                {
                    if (address == null) return null;
                }
            }

            if (!ushort.TryParse(parts[1], out ushort port))
            {
                //RainMeadow.Debug("Invalid port format: " + parts[1]);
                return null;
            }

            if (address == null) return null;
            return new IPEndPoint(address, port);
        }
    }
}
