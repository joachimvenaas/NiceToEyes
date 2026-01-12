using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace NiceToEyes
{
    public static class IconGenerator
    {
        public static Icon CreateMoonIcon(int size = 32)
        {
            using var bitmap = CreateMoonBitmap(size);
            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }
        
        private static Bitmap CreateMoonBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            float scale = size / 32f;
            
            // Draw a crescent moon - brighter yellow/gold color
            using var moonBrush = new SolidBrush(Color.FromArgb(255, 240, 220, 80));
            g.FillEllipse(moonBrush, 4 * scale, 4 * scale, 24 * scale, 24 * scale);
            
            // Create crescent effect with dark background color
            using var shadowBrush = new SolidBrush(Color.FromArgb(255, 45, 45, 48));
            g.FillEllipse(shadowBrush, 10 * scale, 2 * scale, 22 * scale, 22 * scale);
            
            // Add a subtle glow effect
            using var glowPen = new Pen(Color.FromArgb(80, 240, 220, 80), 1 * scale);
            g.DrawEllipse(glowPen, 3 * scale, 3 * scale, 26 * scale, 26 * scale);
            
            return bitmap;
        }
        
        public static void SaveMultiSizeIcon(string path)
        {
            var sizes = new[] { 16, 32, 48, 256 };
            var bitmaps = new List<Bitmap>();
            
            foreach (var size in sizes)
            {
                bitmaps.Add(CreateMoonBitmap(size));
            }
            
            SaveAsIco(bitmaps, path);
            
            foreach (var bmp in bitmaps)
                bmp.Dispose();
        }
        
        private static void SaveAsIco(List<Bitmap> images, string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            
            // ICO header
            writer.Write((short)0);           // Reserved
            writer.Write((short)1);           // Type: 1 = ICO
            writer.Write((short)images.Count); // Number of images
            
            // Prepare image data
            var imageData = new List<byte[]>();
            foreach (var img in images)
            {
                using var ms = new MemoryStream();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                imageData.Add(ms.ToArray());
            }
            
            // Calculate offset (header = 6 bytes, each entry = 16 bytes)
            int offset = 6 + (16 * images.Count);
            
            // Write directory entries
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i];
                var data = imageData[i];
                
                writer.Write((byte)(img.Width >= 256 ? 0 : img.Width));
                writer.Write((byte)(img.Height >= 256 ? 0 : img.Height));
                writer.Write((byte)0);    // Color palette
                writer.Write((byte)0);    // Reserved
                writer.Write((short)1);   // Color planes
                writer.Write((short)32);  // Bits per pixel
                writer.Write(data.Length);
                writer.Write(offset);
                
                offset += data.Length;
            }
            
            // Write image data
            foreach (var data in imageData)
            {
                writer.Write(data);
            }
        }
    }
}
