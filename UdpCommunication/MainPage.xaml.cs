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
        ushort autoSendPort = 8721;
        IPEndPoint? lastEndPoint = null;
        bool running = true;

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

            new Thread(() =>
            {
                try
                {
                    var buffer = new byte[1024];
                    while (running)
                    {
                        if (socket.Poll(Timeout.InfiniteTimeSpan, SelectMode.SelectRead))
                        {
                            while (socket.Available > buffer.Length)
                            {
                                buffer = new byte[socket.Available];
                            }
                            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                            int readBytesLength = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                            if (AutoSendCheckbox.IsChecked)
                            {
                                if (remoteEndPoint is IPEndPoint endPoint)
                                {
                                    if (endPoint.Port == autoSendPort)
                                    {
                                        if (lastEndPoint != null)
                                        {
                                            socket.SendTo(buffer, readBytesLength, SocketFlags.None, lastEndPoint);
                                        }
                                        else
                                        {
                                            Application.Current?.Dispatcher.Dispatch(() =>
                                            {
                                                ReceiveMessageEditor.Text += $"丢弃来自{autoSendPort}端口的数据，因为还没有发送到这个端口的数据\n";
                                            });
                                        }
                                    }
                                    else
                                    {
                                        if (lastEndPoint?.Port != endPoint.Port)
                                        {
                                            lastEndPoint = endPoint;
                                            Application.Current?.Dispatcher.Dispatch(() =>
                                            {
                                                ReceiveMessageEditor.Text += $"转发来自{autoSendPort}端口的数据到{endPoint}\n";
                                            });
                                        }
                                        socket.SendTo(buffer, readBytesLength, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, autoSendPort));
                                    }
                                }
                            }
                            else
                            {
                                string msg = Encoding.UTF8.GetString(buffer, 0, readBytesLength);
                                Application.Current?.Dispatcher.Dispatch(() =>
                                {
                                    RemotIpPortEditor.Text = remoteEndPoint.ToString();
                                    ReceiveMessageEditor.Text += $"{remoteEndPoint}:{msg}\n";
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }).Start();
        }

        private async void SendMessageBtn_Clicked(object sender, EventArgs e)
        {
            IPEndPoint? iPEndPoint = IpPortParser.GetEndPointByName(RemotIpPortEditor.Text);
            if (iPEndPoint != null)
            {
                await socket.SendToAsync(Encoding.UTF8.GetBytes(MessageEditor.Text), iPEndPoint);
            }
            else
            {
                ReceiveMessageEditor.Text += $"尝试发送到 {RemotIpPortEditor.Text} 失败，请检查输入是否正确。\n";
            }
        }

        private void AutoSendTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            AutoSendCheckbox.IsChecked = !AutoSendCheckbox.IsChecked;
        }

        private void AutoSendEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ushort.TryParse(AutoSendEditor.Text, out ushort newPort))
            {
                autoSendPort = newPort;
                AutoSendLabel.Text = $"自动转发数据到指定端口{newPort}";
            }
        }

        private void ContentPage_Disappearing(object sender, EventArgs e)
        {
            running = false;
            socket.Close();
        }
    }
}
