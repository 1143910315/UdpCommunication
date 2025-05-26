using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using UdpCommunication.Utils;

namespace UdpCommunication
{
    public partial class MainPage : ContentPage
    {
        readonly Socket socket;
        const int DEFAULT_PORT = 8720;
        const int FIND_PORT_ATTEMPTS = 10;
        readonly int port;
        readonly System.Timers.Timer timer;

        public MainPage()
        {
            InitializeComponent();
            socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true
            };

            port = DEFAULT_PORT;
            int i = 0;
            for (; i < FIND_PORT_ATTEMPTS; i++)
            {
                try
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    break;
                }
                catch (Exception)
                {
                    port++;
                }
            }

            if (i == FIND_PORT_ATTEMPTS)
            {
                throw new Exception("Failed to claim a socket port");
            }
            
            LocalIpPortEditor.Text = socket.LocalEndPoint?.ToString() ?? "发生错误";

            
            // 创建计时器，间隔 100ms
            timer = new(100);
            // 绑定事件
            timer.Elapsed += (e, arg) =>
            {
                while (socket.Available > 0)
                {
                    try
                    {
                        var buffer = new byte[socket.Available];
                        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 8720);
                        socket.ReceiveFrom(buffer, ref remoteEndPoint);
                        string msg = Encoding.UTF8.GetString(buffer);
                        Application.Current?.Dispatcher.Dispatch(() =>
                        {
                            RemotIpPortEditor.Text = remoteEndPoint.ToString();
                            ReceiveMessageEditor.Text += $"{remoteEndPoint}:{msg}\n";
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
                timer.Start();
            };
            timer.AutoReset = false; // 是否重复触发
            timer.Start();
        }

        private async void SendMessageBtn_Clicked(object sender, EventArgs e)
        {
            await socket.SendToAsync(Encoding.UTF8.GetBytes(MessageEditor.Text), IpPortParser.ParseIpPort(RemotIpPortEditor.Text));
        }
    }
}
