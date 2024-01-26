using System;
using System.Runtime.InteropServices;
using FramePFX.Editors.Rendering;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Effects {
    public class PixilateEffect : VideoEffect {
        public int BlocKSize = 4;

        public override void PostProcessFrame(RenderContext rc) {
            base.PostProcessFrame(rc);

            int width = rc.ImageInfo.Width;
            int height = rc.ImageInfo.Height;
            int blockSize = this.BlocKSize;
            int rowBytes = rc.ImageInfo.RowBytes;
            IntPtr lpPixels = rc.Pixmap.GetPixels();

            for (int y = 0; y < height; y += blockSize) {
                for (int x = 0; x < width; x += blockSize) {
                    int blockWidth = Math.Min(blockSize, width - x);
                    int blockHeight = Math.Min(blockSize, height - y);

                    long totalR = 0, totalG = 0, totalB = 0, totalA = 0;
                    for (int dy = 0; dy < blockHeight; dy++) {
                        for (int dx = 0; dx < blockWidth; dx++) {
                            int pixelOffset = (y + dy) * rowBytes + (x + dx) * 4;
                            int pixel = Marshal.ReadInt32(lpPixels + pixelOffset);
                            SKColor colour = new SKColor((uint) pixel);

                            totalB += colour.Blue;
                            totalG += colour.Green;
                            totalR += colour.Red;
                            totalA += colour.Alpha;
                        }
                    }

                    byte avgB = (byte) (totalB / (blockWidth * blockHeight));
                    byte avgG = (byte) (totalG / (blockWidth * blockHeight));
                    byte avgR = (byte) (totalR / (blockWidth * blockHeight));
                    byte avgA = (byte) (totalA / (blockWidth * blockHeight));

                    for (int dy = 0; dy < blockHeight; dy++) {
                        for (int dx = 0; dx < blockWidth; dx++) {
                            int pixelOffset = (y + dy) * rowBytes + (x + dx) * 4;
                            int pixel = avgB | (avgG << 8) | (avgR << 16) | (avgA << 24);
                            Marshal.WriteInt32(lpPixels, pixelOffset, pixel);
                            // Marshal.WriteByte(lpPixels, pixelOffset, avgB);
                            // Marshal.WriteByte(lpPixels, pixelOffset + 1, avgG);
                            // Marshal.WriteByte(lpPixels, pixelOffset + 2, avgR);
                            // Marshal.WriteByte(lpPixels, pixelOffset + 3, avgA);
                        }
                    }
                }
            }
        }
    }
}