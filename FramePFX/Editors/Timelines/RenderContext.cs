using System.Numerics;
using SkiaSharp;

namespace FramePFX.Editors.Timelines {
    public sealed class RenderContext {
        /// <summary>
        /// The target render surface
        /// </summary>
        public SKSurface Surface { get; }

        /// <summary>
        /// The surface's canvas
        /// </summary>
        public SKCanvas Canvas { get; }

        /// <summary>
        /// The image info about the surface
        /// </summary>
        public SKImageInfo FrameInfo { get; }

        /// <summary>
        /// The size of the rendering canvas, e.g. 1920,1080
        /// </summary>
        public Vector2 FrameSize { get; }

        /// <summary>
        /// Gets the <see cref="SKFilterQuality"/> which is based on the <see cref="RenderQuality"/>
        /// </summary>
        public SKFilterQuality RenderFilterQuality;

        /// <summary>
        /// The timeline render depth. Rendering the main timeline only means that this value only ever reaches a value of 1
        /// </summary>
        public int Depth;

        public RenderContext(SKSurface surface, SKCanvas canvas, SKImageInfo frameInfo) {
            this.Surface = surface;
            this.Canvas = canvas;
            this.FrameInfo = frameInfo;
            this.FrameSize = new Vector2(frameInfo.Width, frameInfo.Height);
        }

        /// <summary>
        /// Clears the context's drawing canvas
        /// </summary>
        public void ClearPixels() {
            this.Canvas.Clear(SKColors.Black);
        }
    }
}