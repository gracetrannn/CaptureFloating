using System;
using System.Drawing;
using System.Windows;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Input;
using System.IO;

namespace WebcamApp
{
    public partial class MainWindow : Window
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
            {
                CameraSelector.Items.Add(device.Name);
            }

            if (videoDevices.Count > 0)
            {
                CameraSelector.SelectedIndex = 0;
                startCamera(videoDevices[0].MonikerString);
            }
        }

        private void CameraSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (videoSource != null)
            {
                videoSource.Stop();
            }

            int selectedIndex = CameraSelector.SelectedIndex;
            if (selectedIndex >= 0)
            {
                startCamera(videoDevices[selectedIndex].MonikerString);
            }
        }

        private void startCamera(string monikerString)
        {
            videoSource = new VideoCaptureDevice(monikerString);
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            videoSource.Start();
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Console.WriteLine("New Frame Captured"); // Debugging log

            BitmapImage bi;
            using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
            {
                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = memoryStream;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                }
            }
            bi.Freeze(); // Ensure this works across threads
            _ = Dispatcher.BeginInvoke(new Action(() => VideoView.Source = bi));
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (videoSource != null)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
