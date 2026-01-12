using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NiceToEyes
{
    public enum GripType
    {
        Move,
        Resize,
        Close
    }

    public partial class GripWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private readonly GripType _gripType;
        private Point _dragStart;
        private bool _isDragging;
        private IntPtr _hwnd;

        public event Action<double, double>? OnDrag;
        public event Action? OnCloseClick;
        
        public IntPtr Hwnd => _hwnd;

        public GripWindow(GripType gripType)
        {
            InitializeComponent();
            _gripType = gripType;
            Loaded += GripWindow_Loaded;
            SetupGripVisual();
        }

        private void GripWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
        }

        private void SetupGripVisual()
        {
            GripContainer.Children.Clear();

            if (_gripType == GripType.Move)
            {
                Width = 40;
                Height = 40;
                Cursor = Cursors.SizeAll;

                // Semi-transparent background for visibility
                var background = new Ellipse
                {
                    Width = 36,
                    Height = 36,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                GripContainer.Children.Add(background);

                // Four-directional arrow icon for move (centered in 40x40)
                // Arrows pointing up, down, left, right from center
                var path = new Path
                {
                    // Center cross with arrow heads
                    Data = Geometry.Parse(
                        "M 20,8 L 20,32 " +  // Vertical line
                        "M 8,20 L 32,20 " +  // Horizontal line
                        "M 20,8 L 16,14 M 20,8 L 24,14 " +  // Up arrow
                        "M 20,32 L 16,26 M 20,32 L 24,26 " +  // Down arrow
                        "M 8,20 L 14,16 M 8,20 L 14,24 " +  // Left arrow
                        "M 32,20 L 26,16 M 32,20 L 26,24"),  // Right arrow
                    Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    StrokeThickness = 2.5,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                GripContainer.Children.Add(path);
                
                // Add mouse event handlers for dragging
                GripContainer.MouseLeftButtonDown += Grip_MouseLeftButtonDown;
                GripContainer.MouseLeftButtonUp += Grip_MouseLeftButtonUp;
                GripContainer.MouseMove += Grip_MouseMove;
            }
            else if (_gripType == GripType.Resize)
            {
                Width = 36;
                Height = 36;
                Cursor = Cursors.SizeNWSE;

                // Semi-transparent background for visibility
                var background = new Rectangle
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    RadiusX = 4,
                    RadiusY = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                GripContainer.Children.Add(background);

                // Diagonal resize lines (centered in 36x36)
                var path = new Path
                {
                    Data = Geometry.Parse("M 6,30 L 30,6 M 12,30 L 30,12 M 18,30 L 30,18 M 24,30 L 30,24"),
                    Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    StrokeThickness = 2.5,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                GripContainer.Children.Add(path);
                
                // Add mouse event handlers for dragging
                GripContainer.MouseLeftButtonDown += Grip_MouseLeftButtonDown;
                GripContainer.MouseLeftButtonUp += Grip_MouseLeftButtonUp;
                GripContainer.MouseMove += Grip_MouseMove;
            }
            else if (_gripType == GripType.Close)
            {
                Width = 36;
                Height = 36;
                Cursor = Cursors.Hand;

                // Semi-transparent background for visibility
                var background = new Ellipse
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 80, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                GripContainer.Children.Add(background);

                // X icon for close (centered in 36x36)
                var path = new Path
                {
                    Data = Geometry.Parse("M 10,10 L 26,26 M 26,10 L 10,26"),
                    Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 100, 100)),
                    StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                GripContainer.Children.Add(path);
                
                // Add click handler for close
                GripContainer.MouseLeftButtonDown += (s, e) => OnCloseClick?.Invoke();
            }
        }

        private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(this);
            GripContainer.CaptureMouse();
        }

        private void Grip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            GripContainer.ReleaseMouseCapture();
        }

        private void Grip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var currentPos = e.GetPosition(this);
            double deltaX = currentPos.X - _dragStart.X;
            double deltaY = currentPos.Y - _dragStart.Y;

            OnDrag?.Invoke(deltaX, deltaY);
        }
    }
}
