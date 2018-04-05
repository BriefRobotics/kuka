using System;
using System.Threading;
using Windows.System.Threading;
using Windows.Media;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Media.Devices;
using Windows.Media.Capture;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;

namespace FaceTracking
{
    public class Tracker
    {
        private readonly CaptureElement display;
        private readonly MediaCapture cam = new MediaCapture();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private FaceTracker tracker;
        private VideoEncodingProperties properties;
        private ThreadPoolTimer timer;

        public event EventHandler<Tuple<IList<DetectedFace>, int, int>> Detected;

        public Tracker(CaptureElement display)
        {
            this.display = display;
        }

        private async void Frame(ThreadPoolTimer timer)
        {
            if (cam.CameraStreamState != CameraStreamState.Streaming) return;
            if (!semaphore.Wait(0)) return;

            try
            {
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame frame = new VideoFrame(InputPixelFormat, (int)properties.Width, (int)properties.Height))
                {
                    await cam.GetPreviewFrameAsync(frame);
                    var faces = await tracker.ProcessNextFrameAsync(frame);
                    var b = frame.SoftwareBitmap;
                    Detected?.Invoke(this, Tuple.Create(faces, b.PixelWidth, b.PixelHeight));
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async void Start(TimeSpan rate)
        {
            tracker = await FaceTracker.CreateAsync();
            cam.Failed += (_, e) => throw new Exception($"Camera failure: {e.Message}");
            await cam.InitializeAsync(new MediaCaptureInitializationSettings() { StreamingCaptureMode = StreamingCaptureMode.Video });
            properties = cam.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            if (display != null) display.Source = cam;
            await cam.StartPreviewAsync();
            timer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(Frame), rate);
        }

        public async void Stop()
        {
            timer?.Cancel();
            await cam.StopPreviewAsync();
        }
    }
}
