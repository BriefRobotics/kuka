using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;

using System.IO;
using System.Net;
using System.Net.Sockets;

namespace FaceTracking
{
    public sealed partial class MainPage : Page
    {
        private readonly SolidColorBrush fill = new SolidColorBrush(Colors.Transparent);
        private readonly SolidColorBrush red = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush yellow = new SolidColorBrush(Colors.Yellow);

        private Tracker tracker;
        private UdpClient udp;

        private void SentFace(Tuple<uint, uint, uint, uint> box)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            if (box == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(1);
                writer.Write(box.Item1);
                writer.Write(box.Item2);
                writer.Write(box.Item3);
                writer.Write(box.Item4);
            }

            udp.Client.Send(ms.ToArray());
        }

        public MainPage()
        {
            udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            udp.Client.Connect("localhost", 7777);

            InitializeComponent();
            tracker = new Tracker(Display);
            tracker.Detected += (_, f) =>
            {
                var faces = f.Item1;
                var w = f.Item2;
                var h = f.Item3;
                this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Canvas.Children.Clear();

                    double actualWidth = this.Canvas.ActualWidth;
                    double actualHeight = this.Canvas.ActualHeight;

                    double widthScale = w / actualWidth;
                    double heightScale = h / actualHeight;

                    var nearestArea = 0u;
                    Tuple<uint, uint, uint, uint> nearestBox = null;
                    foreach (var face in faces)
                    {
                        var box = face.FaceBox;
                        var area = box.Width * box.Height;
                        if (area > nearestArea)
                        {
                            nearestArea = area;
                            nearestBox = Tuple.Create(box.X, box.Y, box.Width, box.Height);
                        }
                    }

                    SentFace(nearestBox);

                    foreach (var face in faces)
                    {
                        var fb = face.FaceBox;
                        Rectangle box = new Rectangle();
                        box.Width = (uint)(fb.Width / widthScale);
                        box.Height = (uint)(fb.Height / heightScale);
                        box.Fill = fill;
                        box.Stroke = fb.Width * fb.Height == nearestArea ? red : yellow;
                        box.StrokeThickness = 4.0;
                        box.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale), 0, 0);
                        this.Canvas.Children.Add(box);
                    }
                }).AsTask().Wait();
            };
            tracker.Start(TimeSpan.FromMilliseconds(66));
        }
    }
}
