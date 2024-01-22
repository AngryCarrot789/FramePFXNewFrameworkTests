using System.Numerics;
using FramePFX.Editors.Rendering;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    /// <summary>
    /// A video clip that draws a basic square, used as a debug video clip mostly
    /// </summary>
    public class VideoClipShape : VideoClip {
        private RenderData renderData;

        public Vector2 Point { get; set; } = new Vector2(10);

        public Vector2 RectSize { get; set; } = new Vector2(100, 40);

        public VideoClipShape() {
        }

        protected override void OnPrepareRender(RenderFrameInfo info, long frame) {
            this.renderData = new RenderData() {
                opacity = this.Opacity,
                shapePos = this.Point,
                shapeSize = this.RectSize,
                colour = this.Track?.Colour ?? SKColors.White
            };
        }

        protected override void OnRenderCore(RenderFrameInfo info, SKSurface surface, long frame) {
            RenderData o = this.renderData;
            using (SKPaint paint = new SKPaint() {Color = o.colour}) {
                surface.Canvas.DrawRect(o.shapePos.X, o.shapePos.Y, o.shapeSize.X, o.shapeSize.Y, paint);
            }
        }

        private struct RenderData {
            public double opacity; // the clip opacity
            public Vector2 shapePos;
            public Vector2 shapeSize;
            public SKColor colour;
        }
    }
}