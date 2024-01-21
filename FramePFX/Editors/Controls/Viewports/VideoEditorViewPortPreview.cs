﻿using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using FramePFX.Editors.Rendering;
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
                control.SetProject(newEditor.Project);
            }
        }

        private void OnProjectChanged(VideoEditor editor, Project oldproject, Project newproject) {
            this.SetProject(newproject);
        }

        private void SetProject(Project project) {
            Project oldProject = this.activeProject;
            if (oldProject != null) {
                oldProject.MainTimeline.PlayHeadChanged -= this.OnTimelineSeeked;
                oldProject.RenderManager.FrameRendered -= this.OnFrameAvailable;
            }

            this.activeProject = project;
            if (project != null) {
                project.MainTimeline.PlayHeadChanged += this.OnTimelineSeeked;
                project.RenderManager.FrameRendered += this.OnFrameAvailable;
            }
        }

        private void OnFrameAvailable(RenderManager manager) {
            if (!this.BeginRender(out SKSurface surface)) {
                return;
            }

            // this is horrible... for now

            try {
                surface.Canvas.Clear(SKColors.Black);
                manager.Draw(surface);
            }
            finally {
                this.EndRender();
            }
        }

        private void OnTimelineSeeked(Timeline timeline, long oldFrame, long frame) {
            timeline.Project.RenderManager.InvalidateRender();
        }

        // SaveLayer requires a temporary drawing bitmap, which can slightly
        // decrease performance, so only SaveLayer when absolutely necessary
        private static int SaveLayerForOpacity(SKCanvas canvas, double opacity, ref SKPaint transparency) {
            return canvas.SaveLayer(transparency ?? (transparency = new SKPaint {
                Color = new SKColor(255, 255, 255, RenderUtils.DoubleToByte255(opacity))
            }));
        }

        private static int BeginClipOpacityLayer(SKCanvas canvas, VideoClip clip, ref SKPaint paint) {
            if (clip.UsesCustomOpacityCalculation || Maths.Equals(clip.Opacity, 1d)) {
                return canvas.Save();
            }
            else {
                return SaveLayerForOpacity(canvas, clip.Opacity, ref paint);
            }
        }

        private static int BeginTrackOpacityLayer(SKCanvas canvas, VideoTrack track, ref SKPaint paint) {
            // TODO: optimise this, because it adds about 3ms of extra lag per layer with an opacity less than 1 (due to bitmap allocation obviously)
            return !Maths.Equals(track.Opacity, 1d) ? SaveLayerForOpacity(canvas, track.Opacity, ref paint) : canvas.Save();
        }

        private static void EndOpacityLayer(SKCanvas canvas, int count, ref SKPaint paint) {
            canvas.RestoreToCount(count);
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
