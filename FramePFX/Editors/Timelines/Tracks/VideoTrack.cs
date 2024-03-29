using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Rendering;
using FramePFX.Editors.Timelines.Clips;
using FramePFX.Editors.Timelines.Effects;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks {
    public delegate void VideoTrackEventHandler(VideoTrack track);

    public class VideoTrack : Track {
        public static readonly ParameterDouble OpacityParameter =
            Parameter.RegisterDouble(
                typeof(VideoTrack),
                nameof(VideoTrack),
                "Opacity",
                new ParameterDescriptorDouble(1, 0, 1),
                ValueAccessors.LinqExpression<double>(typeof(VideoTrack), nameof(Opacity)),
                ParameterFlags.InvalidatesRender);

        public static readonly ParameterBoolean VisibleParameter =
            Parameter.RegisterBoolean(
                typeof(VideoTrack),
                nameof(VideoTrack),
                "Visible",
                new ParameterDescriptorBoolean(true),
                ValueAccessors.Reflective<bool>(typeof(VideoTrack), nameof(Visible)),
                ParameterFlags.InvalidatesRender);

        /// <summary> The track opacity. This is an automated parameter and should therefore not be modified directly </summary>
        public double Opacity;

        /// <summary> The track's visibility. This is an automated parameter and should therefore not be modified directly </summary>
        public bool Visible;

        private SKBitmap bitmap;
        private SKPixmap pixmap;
        private SKSurface surface;
        private SKImageInfo surfaceInfo;
        private bool isCanvasClear;
        private VideoClip theClipToRender;
        private List<VideoEffect> theEffectsToApplyToClip;

        public VideoTrack() {
            this.Opacity = OpacityParameter.Descriptor.DefaultValue;
            this.Visible = VisibleParameter.Descriptor.DefaultValue;
        }

        public bool PrepareRenderFrame(SKImageInfo imgInfo, long frame) {
            VideoClip clip = (VideoClip) this.GetClipAtFrame(frame);
            if (clip != null) {
                ReadOnlyCollection<BaseEffect> fxList = clip.Effects;
                List<VideoEffect> effects = new List<VideoEffect>();
                PreRenderContext ctx = new PreRenderContext(imgInfo);
                int fxCount = fxList.Count;
                for (int i = 0; i < fxCount; i++) {
                    if (fxList[i] is VideoEffect videoFx) {
                        videoFx.PrePrepareFrame(ctx, frame);
                        effects.Add(videoFx);
                    }
                }

                clip.PrepareRenderFrame(ctx, frame - clip.FrameSpan.Begin);
                fxCount = effects.Count;
                for (int i = 0; i < fxCount; i++) {
                    effects[i].PostPrepareFrame(ctx, frame);
                }

                this.theClipToRender = clip;
                this.theEffectsToApplyToClip = effects;
                return true;
            }

            return false;
        }

        // CALLED ON A RENDER THREAD
        public void RenderFrame(SKImageInfo imgInfo) {
            if (this.surface == null || this.surfaceInfo != imgInfo) {
                this.surface?.Dispose();
                this.bitmap?.Dispose();
                this.pixmap?.Dispose();
                this.surfaceInfo = imgInfo;
                this.bitmap = new SKBitmap(imgInfo);
                IntPtr ptr = this.bitmap.GetAddress(0, 0);

                this.pixmap = new SKPixmap(imgInfo, ptr, imgInfo.RowBytes);
                this.surface = SKSurface.Create(this.pixmap);
            }

            if (!this.isCanvasClear) {
                this.surface.Canvas.Clear(SKColors.Transparent);
                this.isCanvasClear = true;
            }

            if (this.theClipToRender != null) {
                List<VideoEffect> fxList = this.theEffectsToApplyToClip;
                int fxListCount = fxList.Count;
                Exception renderException = null;
                SKPaint transparency = null;
                int clipOpacityLayer = RenderManager.BeginClipOpacityLayer(this.surface.Canvas, this.theClipToRender, ref transparency);

                RenderContext ctx = new RenderContext(imgInfo, this.surface, this.bitmap, this.pixmap);
                for (int i = 0; i < fxListCount; i++) {
                    fxList[i].PreProcessFrame(ctx);
                }

                try {
                    this.theClipToRender.RenderFrame(ctx);
                }
                catch (Exception e) {
                    renderException = e;
                }

                for (int i = 0; i < fxListCount; i++) {
                    fxList[i].PostProcessFrame(ctx);
                }

                this.theClipToRender = null;
                this.theEffectsToApplyToClip = null;
                this.isCanvasClear = false;
                RenderManager.EndOpacityLayer(this.surface.Canvas, clipOpacityLayer, ref transparency);
                if (renderException != null) {
                    throw renderException;
                }
            }
        }

        public void DrawFrameIntoSurface(SKSurface dstSurface) {
            if (this.surface != null) {
                this.surface.Draw(dstSurface.Canvas, 0, 0, null);
            }
        }

        public override bool IsClipTypeAccepted(Type type) => typeof(VideoClip).IsAssignableFrom(type);

        public override bool IsEffectTypeAccepted(Type effectType) => false;
    }
}