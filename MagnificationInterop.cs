using System;
using System.Runtime.InteropServices;

namespace NiceToEyes
{
    /// <summary>
    /// Provides access to the Windows Magnification API for color inversion effects.
    /// </summary>
    public static class MagnificationInterop
    {
        private const string MagnificationDll = "Magnification.dll";

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagInitialize();

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagUninitialize();

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetColorEffect(IntPtr hwnd, ref ColorEffect pEffect);

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowTransform(IntPtr hwnd, ref Transformation pTransform);

        [DllImport(MagnificationDll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowFilterList(IntPtr hwnd, int dwFilterMode, int count, IntPtr pHWND);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x, int y,
            int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public const string WC_MAGNIFIER = "Magnifier";
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int MS_SHOWMAGNIFIEDCURSOR = 0x0001;
        public const int SW_SHOW = 5;
        public const int SW_HIDE = 0;

        // Filter modes for MagSetWindowFilterList
        public const int MW_FILTERMODE_EXCLUDE = 0;
        public const int MW_FILTERMODE_INCLUDE = 1;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Transformation
        {
            public float m00, m10, m20;
            public float m01, m11, m21;
            public float m02, m12, m22;

            public static Transformation Identity => new Transformation
            {
                m00 = 1.0f, m10 = 0.0f, m20 = 0.0f,
                m01 = 0.0f, m11 = 1.0f, m21 = 0.0f,
                m02 = 0.0f, m12 = 0.0f, m22 = 1.0f
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorEffect
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public float[] transform;

            /// <summary>
            /// Creates an identity color matrix (no change).
            /// </summary>
            public static ColorEffect Identity
            {
                get
                {
                    var effect = new ColorEffect { transform = new float[25] };
                    effect.transform[0] = 1.0f;  // Red to Red
                    effect.transform[6] = 1.0f;  // Green to Green
                    effect.transform[12] = 1.0f; // Blue to Blue
                    effect.transform[18] = 1.0f; // Alpha to Alpha
                    effect.transform[24] = 1.0f; // Bias
                    return effect;
                }
            }

            /// <summary>
            /// Creates an invert color matrix.
            /// </summary>
            public static ColorEffect Invert
            {
                get
                {
                    var effect = new ColorEffect { transform = new float[25] };
                    // Invert matrix: NewColor = 1 - OldColor
                    effect.transform[0] = -1.0f;  // Red to Red (inverted)
                    effect.transform[6] = -1.0f;  // Green to Green (inverted)
                    effect.transform[12] = -1.0f; // Blue to Blue (inverted)
                    effect.transform[18] = 1.0f;  // Alpha unchanged
                    effect.transform[4] = 1.0f;   // Red bias (add 1 after inverting)
                    effect.transform[9] = 1.0f;   // Green bias
                    effect.transform[14] = 1.0f;  // Blue bias
                    effect.transform[24] = 1.0f;  // Final bias
                    return effect;
                }
            }
        }
    }
}
