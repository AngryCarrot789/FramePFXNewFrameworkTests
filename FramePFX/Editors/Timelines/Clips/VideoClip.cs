using System;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Rendering;
using FramePFX.Editors.Timelines.Effects;
using FramePFX.Utils;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Clips {
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
        public static readonly ParameterDouble OpacityParameter =
            Parameter.RegisterDouble(
                typeof(VideoClip),
                nameof(VideoClip),
                "Opacity",
                new ParameterDescriptorDouble(1, 0, 1),
                ValueAccessors.Reflective<double>(typeof(VideoClip), nameof(Opacity)),
                ParameterFlags.InvalidatesRender);

        private SKMatrix __internalTransformationMatrix;
        private bool isMatrixDirty;

        public double Opacity;

        public byte OpacityByte => RenderUtils.DoubleToByte255(this.Opacity);

        /// <summary>
        /// Returns true if this clip handles its own opacity calculations in order for a more
        /// efficient render. Returns false if it should be handled automatically using an offscreen buffer
        /// </summary>
        public bool UsesCustomOpacityCalculation { get; protected set; }

        /// <summary>
        /// This video clip's transformation matrix, which is applied before it is rendered (if
        /// <see cref="OnBeginRender"/> returns true of course). This is calculated by one or
        /// more <see cref="MotionEffect"/> instances, where each instances' matrix is concatenated
        /// in their orders in our effect list
        /// </summary>
        public SKMatrix TransformationMatrix {
            get {
                if (this.isMatrixDirty)
                    this.CookTransformationMatrix();
                return this.__internalTransformationMatrix;
            }
        }

        protected VideoClip() {
            this.Opacity = OpacityParameter.Descriptor.DefaultValue;
        }


        public override bool IsEffectTypeAccepted(Type effectType) {
            return effectType.instanceof(typeof(VideoEffect));
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
        /// Prepares this clip for rendering. This is called on the main thread, and allows rendering data
        /// to be cached locally so that it can be accessed safely by a render thread in <see cref="RenderFrame"/>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="frame">The play head frame, relative to this clip. This will always be within range of our span</param>
        public abstract void PrepareRenderFrame(PreRenderContext ctx, long frame);

        /// <summary>
        /// Renders this clip using the given rendering context data. This is called on a randomly
        /// assigned rendering thread, therefore, this method should not access un-synchronised clip data
        /// </summary>
        /// <param name="rc">The rendering context, containing things such as the surface and canvas to draw to</param>
        public abstract void RenderFrame(RenderContext rc);

        public void InvalidateTransformationMatrix() {
            this.isMatrixDirty = true;
            this.InvalidateRender();
        }

        private void CookTransformationMatrix() {
            SKMatrix matrix = SKMatrix.Identity;
            foreach (BaseEffect effect in this.Effects) {
                if (effect is ITransformationEffect) {
                    matrix = matrix.PreConcat(((ITransformationEffect) effect).TransformationMatrix);
                }
            }

            this.__internalTransformationMatrix = matrix;
            this.isMatrixDirty = false;
        }
    }
}