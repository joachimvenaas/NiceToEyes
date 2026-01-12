using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NiceToEyes
{
    public partial class OverlayWindow : Window
    {
        // Windows API constants for click-through functionality
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private IntPtr _hwnd;
        private GripWindow? _moveGrip;
        private GripWindow? _resizeGrip;
        private GripWindow? _closeGrip;

        public event Action? OnOverlayHidden;

        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OverlayWindow_Loaded;
            LocationChanged += OverlayWindow_PositionChanged;
            SizeChanged += OverlayWindow_PositionChanged;
            Closed += OverlayWindow_Closed;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            
            // Set as tool window and always click-through
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            
            // Create the grip windows
            CreateGripWindows();
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

        private void OverlayWindow_PositionChanged(object? sender, EventArgs e)
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

        private void OverlayWindow_Closed(object? sender, EventArgs e)
        {
            _moveGrip?.Close();
            _closeGrip?.Close();
            _resizeGrip?.Close();
        }

        /// <summary>
        /// Shows or hides the grip controls.
        /// </summary>
        public void SetGripsVisible(bool visible)
        {
            if (_moveGrip != null)
            {
                if (visible) _moveGrip.Show();
                else _moveGrip.Hide();
            }

            if (_resizeGrip != null)
            {
                if (visible) _resizeGrip.Show();
                else _resizeGrip.Hide();
            }
        }

        /// <summary>
        /// Sets the opacity of the overlay (0.0 to 1.0).
        /// </summary>
        public void SetOverlayOpacity(double opacity)
        {
            opacity = Math.Clamp(opacity, 0.0, 1.0);
            var color = ((SolidColorBrush)OverlayBorder.Background).Color;
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255),
                color.R,
                color.G,
                color.B));
        }

        /// <summary>
        /// Sets the color of the overlay.
        /// </summary>
        public void SetOverlayColor(Color color)
        {
            var currentBrush = (SolidColorBrush)OverlayBorder.Background;
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(
                currentBrush.Color.A,
                color.R,
                color.G,
                color.B));
        }

        /// <summary>
        /// Gets the current opacity of the overlay.
        /// </summary>
        public double GetOverlayOpacity()
        {
            var color = ((SolidColorBrush)OverlayBorder.Background).Color;
            return color.A / 255.0;
        }

        /// <summary>
        /// Gets the current color of the overlay.
        /// </summary>
        public Color GetOverlayColor()
        {
            return ((SolidColorBrush)OverlayBorder.Background).Color;
        }

        /// <summary>
        /// Hides the overlay and grips.
        /// </summary>
        public new void Hide()
        {
            base.Hide();
            _moveGrip?.Hide();
            _closeGrip?.Hide();
            _resizeGrip?.Hide();
        }

        /// <summary>
        /// Shows the overlay and grips.
        /// </summary>
        public new void Show()
        {
            base.Show();
            _moveGrip?.Show();
            _closeGrip?.Show();
            _resizeGrip?.Show();
            UpdateGripPositions();
        }
    }
}
