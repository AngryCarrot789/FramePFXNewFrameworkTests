using System.Windows;
using System.Windows.Media;
using FramePFX.Editors.Timelines;

namespace FramePFX.Editors.Controls.Viewports {
    public class AsyncEditorViewPort : SKAsyncViewPort {
        private const double thickness = 2.5d;
        private const double half_thickness = thickness / 2d;
        private readonly Pen OutlinePen = new Pen(Brushes.Orange, 2.5f);

        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register(
                "Timeline",
                typeof(Timeline),
                typeof(AsyncEditorViewPort),
                new PropertyMetadata(null, (d, e) => ((AsyncEditorViewPort)d).OnTimelineChanged((Timeline)e.OldValue, (Timeline)e.NewValue)));

        public Timeline Timeline {
            get => (Timeline)this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public AsyncEditorViewPort() {
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            // if (oldTimeline != null)
            //     oldTimeline.ClipSelectionChanged -= this.OnClipSelectionChanged;
            // if (newTimeline != null)
            //     newTimeline.ClipSelectionChanged += this.OnClipSelectionChanged;
        }

        private void OnClipSelectionChanged(Timeline timeline) {
            this.InvalidateVisual();
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
