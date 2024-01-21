using System.Numerics;
using FramePFX.Editors.Rendering;
using FramePFX.Utils;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void VideoClipEventHandler(VideoClip track);

    /// <summary>
    /// The base class for all clips that produce video data, whether that be an image, a video frame at some time, text, particles, etc.
    /// </summary>
    public abstract class VideoClip : Clip {
        private double opacity;

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

        /// <summary>
        /// Tells this clip to prepare its proxy data to be rendered on the render thread
        /// </summary>
        /// <param name="info">The rendering info</param>
        public void BeginRenderFrame(RenderFrameInfo info) {
            this.OnBeginRender(info, info.PlayHeadFrame - this.FrameSpan.Begin);
        }

        /// <summary>
        /// Renders this clip based upon its rendering proxy data
        /// </summary>
        /// <param name="info"></param>
        /// <param name="surface"></param>
        public void RenderFrame(RenderFrameInfo info, SKSurface surface) {
            this.OnRenderCore(info, surface, info.PlayHeadFrame - this.FrameSpan.Begin);
        }

        protected abstract void OnBeginRender(RenderFrameInfo info, long frame);

        protected abstract void OnRenderCore(RenderFrameInfo info, SKSurface surface, long frame);
    }
}