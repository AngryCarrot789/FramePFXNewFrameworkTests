using System;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Rendering;
using FramePFX.Editors.Timelines.Tracks.Clips;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks {
    public delegate void VideoTrackEventHandler(VideoTrack track);

    public class VideoTrack : Track {
        public static readonly ParameterDouble OpacityParameter = Parameter.RegisterDouble(typeof(VideoTrack), nameof(VideoTrack), "Opacity", new ParameterDescriptorDouble(1, 0, 1), t => ((VideoTrack) t).Opacity, (t, v) => ((VideoTrack) t).Opacity = v);
        public static readonly ParameterBoolean VisibleParameter = Parameter.RegisterBoolean(typeof(VideoTrack), nameof(VideoTrack), "Visible", new ParameterDescriptorBoolean(true), t => ((VideoTrack) t).Visible, (t, v) => ((VideoTrack) t).Visible = v);

        /// <summary> The track opacity. This is an automated parameter and should therefore not be modified directly </summary>
        public double Opacity;

        /// <summary> The track's visibility. This is an automated parameter and should therefore not be modified directly </summary>
        public bool Visible;

        private SKSurface surface;
        private SKImageInfo surfaceInfo;
        private bool isCanvasClear;

        private VideoClip theClipToRender;

        public VideoTrack() {
            this.Opacity = OpacityParameter.Descriptor.DefaultValue;
            this.Visible = VisibleParameter.Descriptor.DefaultValue;
        }

        public bool BeginRenderFrame(RenderFrameInfo info) {
            VideoClip clip = (VideoClip) this.GetClipAtFrame(info.PlayHeadFrame);
            if (clip != null) {
                clip.PrepareRenderFrame(info);
                this.theClipToRender = clip;
                return true;
            }

            return false;
        }

        // CALLED ON A RENDER THREAD
        public void RenderFrame(RenderFrameInfo info) {
            if (this.surface == null || this.surfaceInfo != info.ImageInfo) {
                this.surface?.Dispose();
                this.surfaceInfo = info.ImageInfo;
                this.surface = SKSurface.Create(info.ImageInfo);
            }

            if (!this.isCanvasClear) {
                this.surface.Canvas.Clear(SKColors.Transparent);
                this.isCanvasClear = true;
            }

            if (this.theClipToRender != null) {
                SKPaint transparency = null;
                int count = RenderManager.BeginClipOpacityLayer(this.surface.Canvas, this.theClipToRender, ref transparency);
                this.theClipToRender.RenderFrame(info, this.surface);
                this.theClipToRender = null;
                this.isCanvasClear = false;
                RenderManager.EndOpacityLayer(this.surface.Canvas, count, ref transparency);
            }
        }

        public void DrawFrameIntoSurface(SKSurface dstSurface) {
            if (this.surface != null) {
                this.surface.Draw(dstSurface.Canvas, 0, 0, null);
            }
        }

        public override bool IsClipTypeAccepted(Type type) {
            return typeof(VideoClip).IsAssignableFrom(type);
        }
    }
}