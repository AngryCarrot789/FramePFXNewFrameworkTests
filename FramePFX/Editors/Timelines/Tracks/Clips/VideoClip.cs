using System.Numerics;
using FramePFX.Editors.Media;
using FramePFX.Utils;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void VideoClipEventHandler(VideoClip track);

    public class VideoClip : Clip {
        private double opacity;

        public Vector2 Point { get; set; } = new Vector2(10);

        public Vector2 RectSize { get; set; } = new Vector2(100, 40);

        public bool UsesCustomOpacityCalculation { get; set; }

        public double Opacity {
            get => this.opacity;
            set {
                if (Maths.Equals(this.opacity, value))
                    return;
                this.opacity = value;
                this.OpacityChanged?.Invoke(this);
            }
        }

        public event VideoClipEventHandler OpacityChanged;

        public VideoClip() {
            this.opacity = 1.0;
        }

        public void Render(RenderContext ctx) {
            using (SKPaint paint = new SKPaint() {Color = this.Track.Colour}) {
                ctx.Canvas.DrawRect(this.Point.X, this.Point.Y, this.RectSize.X, this.RectSize.Y, paint);
            }
        }
    }
}