using System.Windows;
using System.Windows.Media;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Utils;
using SkiaSharp;

namespace FramePFX.Editors.Controls.Viewports {
    /// <summary>
    /// Extends <see cref="SKAsyncViewPort"/> to implement further timeline rendering things, like selected clips
    /// </summary>
    public class VideoEditorViewPortPreview : SKAsyncViewPort {
        private const double thickness = 2.5d;
        private const double half_thickness = thickness / 2d;
        public static readonly DependencyProperty VideoEditorProperty = DependencyProperty.Register("VideoEditor", typeof(VideoEditor), typeof(VideoEditorViewPortPreview), new PropertyMetadata(null, OnVideoEditorChanged));

        public VideoEditor VideoEditor {
            get => (VideoEditor) this.GetValue(VideoEditorProperty);
            set => this.SetValue(VideoEditorProperty, value);
        }

        private readonly Pen OutlinePen = new Pen(Brushes.Orange, 2.5f);

        private Project activeProject;

        public VideoEditorViewPortPreview() {

        }

        private static void OnVideoEditorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VideoEditorViewPortPreview control = (VideoEditorViewPortPreview) d;
            if (e.OldValue is VideoEditor oldEditor) {
                oldEditor.ProjectChanged -= control.OnProjectChanged;
            }

            if (e.NewValue is VideoEditor newEditor) {
                newEditor.ProjectChanged += control.OnProjectChanged;
                control.SetProject(newEditor.CurrentProject);
            }
        }

        private void OnProjectChanged(VideoEditor editor, Project oldproject, Project newproject) {
            this.SetProject(newproject);
        }

        private void SetProject(Project project) {
            Project oldProject = this.activeProject;
            if (oldProject != null) {
                oldProject.MainTimeline.PlayHeadChanged -= this.OnTimelineSeeked;
            }

            this.activeProject = project;
            if (project != null) {
                project.MainTimeline.PlayHeadChanged += this.OnTimelineSeeked;
            }
        }

        private void OnTimelineSeeked(Timeline timeline, long oldFrame, long frame) {
            if (!this.BeginRender(out SKSurface surface)) {
                return;
            }

            // this is horrible... for now

            try {
                RenderContext render = new RenderContext(surface, surface.Canvas, this.FrameInfo);
                render.ClearPixels();

                int timelineCanvasSaveIndex = render.Canvas.Save();
                try {
                    render.Canvas.ClipRect(new SKRect(0, 0, render.FrameSize.X, render.FrameSize.Y));

                    SKPaint trackPaint = null;
                    SKPaint clipPaint = null;
                    foreach (Track track in timeline.Tracks) {
                        if (!(track is VideoTrack videoTrack))
                            continue;

                        int trackSaveCount = BeginTrackOpacityLayer(render, videoTrack, ref trackPaint);
                        foreach (VideoClip clip in videoTrack.Clips) {
                            if (clip.FrameSpan.Intersects(frame)) {
                                int clipSaveCount = BeginClipOpacityLayer(render, clip, ref clipPaint);
                                clip.Render(render);
                                EndOpacityLayer(render, clipSaveCount, ref clipPaint);
                            }
                        }

                        EndOpacityLayer(render, trackSaveCount, ref trackPaint);
                    }
                }
                finally {
                    render.Canvas.RestoreToCount(timelineCanvasSaveIndex);
                }
            }
            finally {
                this.EndRender();
            }
        }

        // SaveLayer requires a temporary drawing bitmap, which can slightly
        // decrease performance, so only SaveLayer when absolutely necessary
        private static int SaveLayerForOpacity(SKCanvas canvas, double opacity, ref SKPaint transparency) {
            return canvas.SaveLayer(transparency ?? (transparency = new SKPaint {
                Color = new SKColor(255, 255, 255, RenderUtils.DoubleToByte255(opacity))
            }));
        }

        private static int BeginClipOpacityLayer(RenderContext render, VideoClip clip, ref SKPaint paint) {
            if (clip.UsesCustomOpacityCalculation || Maths.Equals(clip.Opacity, 1d)) {
                return render.Canvas.Save();
            }
            else {
                return SaveLayerForOpacity(render.Canvas, clip.Opacity, ref paint);
            }
        }

        private static int BeginTrackOpacityLayer(RenderContext render, VideoTrack track, ref SKPaint paint) {
            return !Maths.Equals(track.Opacity, 1d)
                // TODO: optimise this, because it adds about 3ms of extra lag per layer with an opacity less than 1
                // (due to bitmap allocation obviously). Not even
                ? SaveLayerForOpacity(render.Canvas, track.Opacity, ref paint)
                : render.Canvas.Save();
        }

        private static void EndOpacityLayer(RenderContext render, int count, ref SKPaint paint) {
            render.Canvas.RestoreToCount(count);
            if (paint != null) {
                paint.Dispose();
                paint = null;
            }
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);
            // if (this.Timeline is Timeline timeline) {
            //     foreach (Track track in timeline.Tracks) {
            //         foreach (Clip clip in track.GetSelectedClipsAtFrame(timeline.PlayHeadPosition)) {
            //             if (!(clip is VideoClip) || !(((VideoClip)clip.Model).GetFrameSize() is Vector2 frameSize)) {
            //                 continue;
            //             }
            // 
            //             SKRect rect = ((VideoClip)clip.Model).TransformationMatrix.MapRect(frameSize.ToRectAsSize(0, 0));
            //             Point pos = new Point(Math.Floor(rect.Left) - half_thickness, Math.Floor(rect.Top) - half_thickness);
            //             Size size = new Size(Math.Ceiling(rect.Width) + thickness, Math.Ceiling(rect.Height) + thickness);
            //             dc.DrawRectangle(null, this.OutlinePen, new Rect(pos, size));
            //         }
            //     }
            // }
        }
    }
}
