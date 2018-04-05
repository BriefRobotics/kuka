using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;

namespace FaceTracking
{
    public sealed partial class MainPage : Page
    {
        private readonly SolidColorBrush fill = new SolidColorBrush(Colors.Transparent);
        private readonly SolidColorBrush red = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush yellow = new SolidColorBrush(Colors.Yellow);

        private Tracker tracker;

        public MainPage()
        {
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

                    var nearest = 0u;
                    foreach (var face in faces)
                    {
                        var box = face.FaceBox;
                        var area = box.Width * box.Height;
                        if (area > nearest)
                        {
                            nearest = area;
                        }
                    }

                    foreach (var face in faces)
                    {
                        var fb = face.FaceBox;
                        Rectangle box = new Rectangle();
                        box.Width = (uint)(fb.Width / widthScale);
                        box.Height = (uint)(fb.Height / heightScale);
                        box.Fill = fill;
                        box.Stroke = fb.Width * fb.Height == nearest ? red : yellow;
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
