using System;
using System.Drawing;
using System.Windows;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Runtime.InteropServices;


namespace WebcamApp
{
    public partial class MainWindow : Window
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private const int resizeBorder = 10;
        private const double MouseLeaveBuffer = 20; // Buffer distance in pixels

        private DispatcherTimer mouseLeaveTimer;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            //.this.SourceInitialized += MainWindow_SourceInitialized; // Hook to the window source

            // Initialize the timer
            mouseLeaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Adjust frequency as needed
            };
            mouseLeaveTimer.Tick += MouseLeaveTimer_Tick;
            // Start the timer
            this.Loaded += (s, e) => mouseLeaveTimer.Start();
        }
        private void MouseLeaveTimer_Tick(object sender, EventArgs e)
        {
            var mousePos = GetMousePosition();
            // Calculate window's screen coordinates
            var windowPos = PointToScreen(new System.Windows.Point(0, 0));
            double rightEdge = windowPos.X + this.ActualWidth;
            double bottomEdge = windowPos.Y + this.ActualHeight;

            // Transform mouse position to screen coordinates
            var screenMousePos = new System.Windows.Point(mousePos.X + windowPos.X, mousePos.Y + windowPos.Y);

            // Check if mouse is outside the buffered boundaries
            if (mousePos.X < windowPos.X - MouseLeaveBuffer ||
                mousePos.Y < windowPos.Y - MouseLeaveBuffer ||
                mousePos.X > rightEdge + MouseLeaveBuffer ||
                mousePos.Y > bottomEdge + MouseLeaveBuffer)
            {
                // Implement what happens when the mouse leaves farther away from the region.
                Console.WriteLine("Mouse has left the window region beyond the buffer.");
                SetGridBackgroundBinding();
                CloseButton.Visibility = Visibility.Collapsed;
                CameraSelector.Visibility = Visibility.Collapsed;
            }
        }
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private struct POINT
        {
            public int X;
            public int Y;
        }

        private POINT GetMousePosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
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
                videoSource.NewFrame -= video_NewFrame;
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // Handle double-click to maximize/restore window
            {
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }
            //.this.DragMove(); // Enable window drag
            DragMove();

        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the timer on closing
            if (mouseLeaveTimer != null)
            {
                mouseLeaveTimer.Stop();
            }

            this.Close();

        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            //.ControlPanel.Visibility = Visibility.Visible; MakeWindowVisible();
            mainwin.Background = new SolidColorBrush(Colors.LightGray);
            OutLine.Background = new SolidColorBrush(Colors.LightGray);

            CloseButton.Visibility = Visibility.Visible;
            CameraSelector.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            /*
            var screenMousePos = PointToScreen(Mouse.GetPosition(this));

            // Convert screen coordinates back to window's logical coordinates
            var relativeMousePos = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(screenMousePos);

            // Include buffer zone in the boundary check
            if (relativeMousePos.X <= -MouseLeaveBuffer || relativeMousePos.Y <= -MouseLeaveBuffer ||
                relativeMousePos.X > this.ActualWidth + MouseLeaveBuffer ||
                relativeMousePos.Y > this.ActualHeight + MouseLeaveBuffer)
            {
                
                SetGridBackgroundBinding();
                CloseButton.Visibility = Visibility.Collapsed;
                CameraSelector.Visibility = Visibility.Collapsed;
                
            }
            
            SetGridBackgroundBinding();
            CloseButton.Visibility = Visibility.Collapsed;
            CameraSelector.Visibility = Visibility.Collapsed;
            */
        }
        private void SetGridBackgroundBinding()
        {
            // Assuming 'mainwin' is your Grid defined in XAML
            var binding = new Binding
            {
                Path = new PropertyPath("Background"),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1),
                Mode = BindingMode.OneWay
            };

            // Apply the binding to the Grid's Background property
            BindingOperations.SetBinding(mainwin, Panel.BackgroundProperty, binding);
            BindingOperations.SetBinding(OutLine, Panel.BackgroundProperty, binding);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
            hwndSource.CompositionTarget.RenderMode = RenderMode.Default;
            if (hwndSource != null)
            {
                hwndSource.AddHook(HandleWindowMessages);
            }
        }

        private IntPtr HandleWindowMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCHITTEST = 0x0084;
            if (msg == WM_NCHITTEST)
            {
                handled = true;
                return new IntPtr(BorderHitTest(lParam));
            }
            return IntPtr.Zero;
        }

        private int BorderHitTest(IntPtr lParam)
        {
            int x = (int)(lParam.ToInt32() & 0xFFFF);
            int y = (int)(lParam.ToInt32() >> 16);

            int left = (int)Left;
            int top = (int)Top;
            int right = left + (int)Width;
            int bottom = top + (int)Height;

            // Corner zones
            if (x >= left - resizeBorder && x <= left + resizeBorder && y >= top - resizeBorder && y <= top + resizeBorder)
                return 13; //HTTOPLEFT
            if (x >= right - resizeBorder && x <= right + resizeBorder && y >= top - resizeBorder && y <= top + resizeBorder)
                return 14; //HTTOPRIGHT
            if (x >= left - resizeBorder && x <= left + resizeBorder && y >= bottom - resizeBorder && y <= bottom + resizeBorder)
                return 16; // HTBOTTOMLEFT
            if (x >= right - resizeBorder && x <= right + resizeBorder && y >= bottom - resizeBorder && y <= bottom + resizeBorder)
                return 17; // HTBOTTOMRIGHT

            // Horizontal zones
            if (x >= left - resizeBorder && x <= left + resizeBorder)
                return 10; // HTLEFT
            if (x >= right - resizeBorder && x <= right + resizeBorder)
                return 11; // HTRIGHT

            // Vertical zones
            if (y >= top - resizeBorder && y <= top + resizeBorder)
                return 12; // HTTOP
            if (y >= bottom - resizeBorder && y <= bottom + resizeBorder)
                return 15; // HTBOTTOM

            return 1; // HTCLIENT
        }
        private void VideoView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateEllipseClip();
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateEllipseClip();
        }

        private void UpdateEllipseClip()
        {
#if false
            var width = VideoView.ActualWidth;
            var height = VideoView.ActualHeight;

            // Example: Update where converters are not used
            // Assuming you need to reposition manually if necessary
            var ellipseGeometry = new EllipseGeometry
            {
                Center = new System.Windows.Point(width / 2, height / 2),
                RadiusX = width / 2,
                RadiusY = height / 2
            };

            VideoView.Clip = ellipseGeometry;
#else
            var width = VideoView.ActualWidth;
            var height = VideoView.ActualHeight;

            var cx = width / 2;
            var cy = height / 2;
            if (width > height)
                width = height;
            else
                height = width;
            // Example: Update where converters are not used
            // Assuming you need to reposition manually if necessary
            var ellipseGeometry = new EllipseGeometry
            {
                Center = new System.Windows.Point(cx, cy),
                RadiusX = width / 2,
                RadiusY = height / 2
            };

            VideoView.Clip = ellipseGeometry;
#endif
        }
    }
}
