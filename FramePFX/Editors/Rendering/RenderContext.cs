using System.Numerics;
using SkiaSharp;

namespace FramePFX.Editors.Rendering {
    public readonly struct RenderContext {
        /// <summary>
        /// The image info associated with our <see cref="Surface"/>
        /// </summary>
        public SKImageInfo ImageInfo { get; }

        /// <summary>
        /// The surface used to draw things
        /// </summary>
        public SKSurface Surface { get; }

        /// <summary>
        /// Our <see cref="Surface"/>'s canvas
        /// </summary>
        public SKCanvas Canvas { get; }

        /// <summary>
        /// A vector2 containing our <see cref="ImageInfo"/>'s width and height
        /// </summary>
        public Vector2 FrameSize => new Vector2(this.ImageInfo.Width, this.ImageInfo.Height);

        public RenderContext(SKImageInfo imageInfo, SKSurface surface) {
            this.ImageInfo = imageInfo;
            this.Surface = surface;
            this.Canvas = surface.Canvas;
        }
    }
}