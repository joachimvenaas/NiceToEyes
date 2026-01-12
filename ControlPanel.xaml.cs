using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace NiceToEyes
{
    public partial class ControlPanel : Window
    {
        private OverlayWindow? _overlayWindow;
        private InvertWindow? _invertWindow;
        private bool _isInvertMode = false;
        private Forms.NotifyIcon? _notifyIcon;
        private Forms.ContextMenuStrip? _contextMenu;
        private Icon? _appIcon;

        public ControlPanel()
        {
            InitializeComponent();
            Loaded += ControlPanel_Loaded;
            StateChanged += ControlPanel_StateChanged;
            SetupWindowIcon();
            SetupNotifyIcon();
        }

        private void SetupWindowIcon()
        {
            try
            {
                _appIcon = IconGenerator.CreateMoonIcon();
                Icon = Imaging.CreateBitmapSourceFromHIcon(
                    _appIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                // Fallback to default icon if generation fails
            }
        }

        private void SetupNotifyIcon()
        {
            // Create context menu
            _contextMenu = new Forms.ContextMenuStrip();
            
            var showHideOverlay = new Forms.ToolStripMenuItem("Hide Overlay");
            showHideOverlay.Click += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ToggleOverlay.IsChecked = !ToggleOverlay.IsChecked;
                    ToggleOverlay_Click(ToggleOverlay, new RoutedEventArgs());
                    showHideOverlay.Text = ToggleOverlay.IsChecked == true ? "Hide Overlay" : "Show Overlay";
                });
            };
            _contextMenu.Items.Add(showHideOverlay);
            
            _contextMenu.Items.Add(new Forms.ToolStripSeparator());
            
            var openPanel = new Forms.ToolStripMenuItem("Open Control Panel");
            openPanel.Click += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            };
            _contextMenu.Items.Add(openPanel);
            
            _contextMenu.Items.Add(new Forms.ToolStripSeparator());
            
            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _notifyIcon?.Dispose();
                    Application.Current.Shutdown();
                });
            };
            _contextMenu.Items.Add(exitItem);
            
            // Create custom moon icon
            var moonIcon = IconGenerator.CreateMoonIcon();
            
            // Create notify icon
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = moonIcon,
                Visible = true,
                Text = "Nice To Eyes",
                ContextMenuStrip = _contextMenu
            };
            
            // Double-click to open control panel
            _notifyIcon.DoubleClick += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            };
            
            // Update menu text when opening
            _contextMenu.Opening += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    showHideOverlay.Text = ToggleOverlay.IsChecked == true ? "Hide Overlay" : "Show Overlay";
                });
            };
        }

        private void ControlPanel_StateChanged(object? sender, EventArgs e)
        {
            // Minimize to tray instead of taskbar
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void ControlPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Create and show the overlay window
            _overlayWindow = new OverlayWindow();
            _overlayWindow.OnOverlayHidden += OnOverlayHiddenByGrip;
            _overlayWindow.Show();
            
            // Create invert window (hidden initially)
            _invertWindow = new InvertWindow();
            _invertWindow.OnOverlayHidden += OnOverlayHiddenByGrip;
            
            // Set initial opacity
            _overlayWindow.SetOverlayOpacity(OpacitySlider.Value / 100.0);
        }

        private void OnOverlayHiddenByGrip()
        {
            // Update the toggle button to reflect the hidden state
            ToggleOverlay.IsChecked = false;
            ToggleOverlay.Content = "Show Overlay";
        }

        private void ToggleOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (ToggleOverlay.IsChecked == true)
            {
                if (_isInvertMode)
                {
                    _invertWindow?.Show();
                }
                else
                {
                    _overlayWindow?.Show();
                }
                ToggleOverlay.Content = "Hide Overlay";
            }
            else
            {
                _overlayWindow?.Hide();
                _invertWindow?.Hide();
                ToggleOverlay.Content = "Show Overlay";
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow == null || OpacityValue == null) return;
            
            int percentage = (int)OpacitySlider.Value;
            OpacityValue.Text = $"{percentage}%";
            _overlayWindow.SetOverlayOpacity(percentage / 100.0);
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorTag)
            {
                if (colorTag == "INVERT")
                {
                    // Switch to invert mode
                    _isInvertMode = true;
                    _overlayWindow?.Hide();
                    
                    if (ToggleOverlay.IsChecked == true)
                    {
                        // Sync position and size
                        if (_overlayWindow != null && _invertWindow != null)
                        {
                            _invertWindow.Left = _overlayWindow.Left;
                            _invertWindow.Top = _overlayWindow.Top;
                            _invertWindow.Width = _overlayWindow.Width;
                            _invertWindow.Height = _overlayWindow.Height;
                        }
                        _invertWindow?.Show();
                    }
                    
                    // Disable opacity slider in invert mode
                    OpacitySlider.IsEnabled = false;
                    OpacityValue.Text = "N/A";
                }
                else
                {
                    // Switch to normal overlay mode
                    _isInvertMode = false;
                    _invertWindow?.Hide();
                    
                    if (ToggleOverlay.IsChecked == true)
                    {
                        // Sync position and size
                        if (_invertWindow != null && _overlayWindow != null)
                        {
                            _overlayWindow.Left = _invertWindow.Left;
                            _overlayWindow.Top = _invertWindow.Top;
                            _overlayWindow.Width = _invertWindow.Width;
                            _overlayWindow.Height = _invertWindow.Height;
                        }
                        _overlayWindow?.Show();
                    }
                    
                    // Enable opacity slider
                    OpacitySlider.IsEnabled = true;
                    OpacityValue.Text = $"{(int)OpacitySlider.Value}%";
                    
                    try
                    {
                        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorTag);
                        _overlayWindow?.SetOverlayColor(color);
                    }
                    catch
                    {
                        _overlayWindow?.SetOverlayColor(Colors.Black);
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up notify icon
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            
            // Close all windows when the control panel closes
            _overlayWindow?.Close();
            _invertWindow?.Close();
        }
    }
}
