using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NiceToEyes
{
    public partial class InvertWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        // Display affinity to exclude window from capture
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height,
            IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        private const int SRCCOPY = 0x00CC0020;

        private IntPtr _hwnd;
        private DispatcherTimer? _updateTimer;
        private GripWindow? _moveGrip;
        private GripWindow? _closeGrip;
        private GripWindow? _resizeGrip;

        public event Action? OnOverlayHidden;

        public InvertWindow()
        {
            InitializeComponent();
            Loaded += InvertWindow_Loaded;
            Closed += InvertWindow_Closed;
            LocationChanged += InvertWindow_PositionChanged;
            SizeChanged += InvertWindow_PositionChanged;
        }

        private void InvertWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // Set as tool window and click-through
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_LAYERED);

            // Exclude this window from screen capture (Windows 10 2004+)
            SetWindowDisplayAffinity(_hwnd, WDA_EXCLUDEFROMCAPTURE);

            CreateGripWindows();

            // Start update timer to refresh the captured screen
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void CreateGripWindows()
        {
            // Create Move Grip (top-left)
            _moveGrip = new GripWindow(GripType.Move);
            _moveGrip.OnDrag += (deltaX, deltaY) =>
            {
                Left += deltaX;
                Top += deltaY;
            };
            _moveGrip.Show();

            // Create Close Grip (top-right)
            _closeGrip = new GripWindow(GripType.Close);
            _closeGrip.OnCloseClick += () =>
            {
                Hide();
                OnOverlayHidden?.Invoke();
            };
            _closeGrip.Show();

            // Create Resize Grip (bottom-right)
            _resizeGrip = new GripWindow(GripType.Resize);
            _resizeGrip.OnDrag += (deltaX, deltaY) =>
            {
                double newWidth = Math.Max(50, Width + deltaX);
                double newHeight = Math.Max(50, Height + deltaY);
                Width = newWidth;
                Height = newHeight;
            };
            _resizeGrip.Show();

            UpdateGripPositions();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            CaptureAndInvert();
        }

        private void CaptureAndInvert()
        {
            try
            {
                // Get DPI scaling
                var source = PresentationSource.FromVisual(this);
                if (source == null) return;

                var transform = source.CompositionTarget.TransformToDevice;
                double dpiX = transform.M11;
                double dpiY = transform.M22;

                int screenX = (int)(Left * dpiX);
                int screenY = (int)(Top * dpiY);
                int width = (int)(Width * dpiX);
                int height = (int)(Height * dpiY);

                if (width <= 0 || height <= 0) return;

                // Capture screen (our window is excluded via WDA_EXCLUDEFROMCAPTURE)
                using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        IntPtr hdcScreen = GetDC(IntPtr.Zero);
                        IntPtr hdcBitmap = graphics.GetHdc();

                        BitBlt(hdcBitmap, 0, 0, width, height, hdcScreen, screenX, screenY, SRCCOPY);

                        graphics.ReleaseHdc(hdcBitmap);
                        ReleaseDC(IntPtr.Zero, hdcScreen);
                    }

                    // Invert colors
                    InvertBitmapColors(bitmap);

                    // Convert to WPF ImageSource
                    var hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        CapturedImage.Source = imageSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch
            {
                // Ignore capture errors
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private unsafe void InvertBitmapColors(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            try
            {
                byte* ptr = (byte*)bitmapData.Scan0;
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;

                for (int i = 0; i < bytes; i += 4)
                {
                    // BGRA format - invert B, G, R, leave A
                    ptr[i] = (byte)(255 - ptr[i]);         // Blue
                    ptr[i + 1] = (byte)(255 - ptr[i + 1]); // Green
                    ptr[i + 2] = (byte)(255 - ptr[i + 2]); // Red
                    // ptr[i + 3] is Alpha - leave unchanged
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        private void InvertWindow_PositionChanged(object? sender, EventArgs e)
        {
            UpdateGripPositions();
        }

        private void UpdateGripPositions()
        {
            if (_moveGrip != null)
            {
                _moveGrip.Left = Left;
                _moveGrip.Top = Top;
            }

            if (_closeGrip != null)
            {
                _closeGrip.Left = Left + Width - _closeGrip.Width;
                _closeGrip.Top = Top;
            }

            if (_resizeGrip != null)
            {
                _resizeGrip.Left = Left + Width - _resizeGrip.Width;
                _resizeGrip.Top = Top + Height - _resizeGrip.Height;
            }
        }

        private void InvertWindow_Closed(object? sender, EventArgs e)
        {
            _updateTimer?.Stop();
            _moveGrip?.Close();
            _closeGrip?.Close();
            _resizeGrip?.Close();
        }

        public new void Hide()
        {
            base.Hide();
            _moveGrip?.Hide();
            _closeGrip?.Hide();
            _resizeGrip?.Hide();
            _updateTimer?.Stop();
        }

        public new void Show()
        {
            base.Show();
            _moveGrip?.Show();
            _closeGrip?.Show();
            _resizeGrip?.Show();
            _updateTimer?.Start();
            UpdateGripPositions();
        }
    }
}
