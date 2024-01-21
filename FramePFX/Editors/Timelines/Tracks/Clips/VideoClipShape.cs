using System.Numerics;
using FramePFX.Editors.Rendering;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    /// <summary>
    /// A video clip that draws a basic square, used as a debug video clip mostly
    /// </summary>
    public class VideoClipShape : VideoClip {
        private RenderProxy renderData;

        public Vector2 Point { get; set; } = new Vector2(10);

        public Vector2 RectSize { get; set; } = new Vector2(100, 40);

        public VideoClipShape() {
        }

        protected override void OnBeginRender(RenderFrameInfo info, long frame) {
            this.renderData = new RenderProxy() {
                opacity = this.Opacity,
                shapePos = this.Point,
                shapeSize = this.RectSize,
                colour = this.Track?.Colour ?? SKColors.White
            };
        }

        protected override void OnRenderCore(RenderFrameInfo info, SKSurface surface, long frame) {
            RenderProxy obj = this.renderData;
            using (SKPaint paint = new SKPaint() {Color = obj.colour}) {
                surface.Canvas.DrawRect(obj.shapePos.X, obj.shapePos.Y, obj.shapeSize.X, obj.shapeSize.Y, paint);
            }
        }

        private struct RenderProxy {
            public double opacity; // the clip opacity
            public Vector2 shapePos;
            public Vector2 shapeSize;
            public SKColor colour;
        }
    }
}