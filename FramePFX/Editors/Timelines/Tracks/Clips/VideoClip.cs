using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Rendering;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void VideoClipEventHandler(VideoClip track);

    /// <summary>
    /// The base class for all clips that produce video data, whether that be an image, a video frame at some time, text, particles, etc.
    /// <para>
    /// When rendering a video clip, there are 3 phases:
    /// </para>
    /// <para>
    /// Preparation on AMT (application main thread). This is where all tracks are processed (bottom to top), the
    /// clip (or clips with a transition) are calculated and then those clips' setup functions are called.
    /// The clip method is <see cref="PrepareRenderFrame"/>, which lets the clip store its current state in
    /// a proxy object which is accessible via the render thread
    /// </para>
    /// <para>
    /// Rendering (on a randomly assigned rendering thread). This is where the clip actually renders its contents.
    /// Since this is done off the main thread on a render-specific thread, it's very important that the clip does
    /// not access any un-synchronised data. The render data should be calculated in the preparation phase
    /// </para>
    /// <para>
    /// Final frame assembly (on render thread). This is where all of the rendered data is assembled into a final
    /// frame. An event (<see cref="RenderManager.FrameRendered"/>) is fired on the application main thread, and the view
    /// port then presents the fully rendered frame to the user
    /// </para>
    /// </summary>
    public abstract class VideoClip : Clip {
        public static readonly ParameterDouble OpacityParameter = Parameter.RegisterDouble(typeof(VideoClip), nameof(VideoClip), "Opacity", new ParameterDescriptorDouble(1, 0, 1), t => ((VideoClip) t).Opacity, (t, v) => ((VideoClip) t).Opacity = v);

        public double Opacity;

        /// <summary>
        /// Returns true if this clip handles its own opacity calculations in order for a more
        /// efficient render. Returns false if it should be handled automatically using an offscreen buffer
        /// </summary>
        public bool UsesCustomOpacityCalculation { get; protected set; }

        public VideoClip() {
            this.Opacity = OpacityParameter.Descriptor.DefaultValue;
        }

        /// <summary>
        /// Propagates the render invalidated state to our project's <see cref="RenderManager"/>
        /// </summary>
        public void InvalidateRender() {
            this.Track?.InvalidateRender();
        }

        protected override void OnFrameSpanChanged(FrameSpan oldSpan, FrameSpan newSpan) {
            base.OnFrameSpanChanged(oldSpan, newSpan);
            this.InvalidateRender();
        }

        /// <summary>
        /// Tells this clip to prepare its proxy data to be rendered on the render thread
        /// </summary>
        /// <param name="info">The rendering info</param>
        public void PrepareRenderFrame(RenderFrameInfo info) {
            this.OnPrepareRender(info, info.PlayHeadFrame - this.FrameSpan.Begin);
        }

        /// <summary>
        /// Renders this clip based upon its rendering proxy data
        /// </summary>
        /// <param name="info"></param>
        /// <param name="surface"></param>
        public void RenderFrame(RenderFrameInfo info, SKSurface surface) {
            this.OnRenderCore(info, surface, info.PlayHeadFrame - this.FrameSpan.Begin);
        }

        protected abstract void OnPrepareRender(RenderFrameInfo info, long frame);

        protected abstract void OnRenderCore(RenderFrameInfo info, SKSurface surface, long frame);
    }
}