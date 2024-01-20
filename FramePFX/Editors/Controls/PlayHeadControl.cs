using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Timelines;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls {
    public class PlayHeadControl : Control {
        private static readonly DependencyPropertyKey FramePropertyKey = DependencyProperty.RegisterReadOnly("Frame", typeof(long), typeof(PlayHeadControl), new PropertyMetadata(0L, (d, e) => ((PlayHeadControl) d).OnPlayHeadFrameChanged(), OnCoerceFrameValue));
        public static readonly DependencyProperty FrameProperty = FramePropertyKey.DependencyProperty;
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(PlayHeadControl), new PropertyMetadata(null, (d, e) => ((PlayHeadControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public long Frame {
            get => (long) this.GetValue(FrameProperty);
            private set => this.SetValue(FramePropertyKey, value);
        }

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        private Thumb PART_ThumbHead;
        private Thumb PART_ThumbBody;
        private bool isDraggingThumb;
        private bool isUpdatingFrameProperty;

        private double UnitZoom => this.Timeline?.Zoom ?? 1.0;

        public PlayHeadControl() {
        }

        static PlayHeadControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (PlayHeadControl), new FrameworkPropertyMetadata(typeof(PlayHeadControl)));
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline != null) {
                oldTimeline.PlayHeadChanged -= this.OnTimelinePlayHeadChanged;
                oldTimeline.ZoomTimeline -= this.OnTimelineZoomed;
            }

            if (newTimeline != null) {
                newTimeline.PlayHeadChanged += this.OnTimelinePlayHeadChanged;
                newTimeline.ZoomTimeline += this.OnTimelineZoomed;
            }
        }

        private void OnTimelinePlayHeadChanged(Timeline timeline, long oldvalue, long newvalue) {
            this.isUpdatingFrameProperty = true;
            try {
                this.Frame = timeline.PlayHeadPosition;
            }
            finally {
                this.isUpdatingFrameProperty = false;
            }
        }

        private void OnTimelineZoomed(Timeline timeline, double oldzoom, double newzoom, ZoomType zoomtype) {
            this.UpdatePixel(timeline.PlayHeadPosition, newzoom);
        }

        public override void OnApplyTemplate() {
            this.PART_ThumbHead = this.GetTemplateChild("PART_ThumbHead") as Thumb;
            this.PART_ThumbBody = this.GetTemplateChild("PART_ThumbBody") as Thumb;
            if (this.PART_ThumbHead != null) {
                this.PART_ThumbHead.DragDelta += this.PART_ThumbOnDragDelta;
            }

            if (this.PART_ThumbBody != null) {
                this.PART_ThumbBody.DragDelta += this.PART_ThumbOnDragDelta;
            }
        }

        private void PART_ThumbOnDragDelta(object sender, DragDeltaEventArgs e) {
            Timeline timeline = this.Timeline;
            if (timeline == null) {
                return;
            }

            long change = (long) (e.HorizontalChange / this.UnitZoom);
            if (change != 0) {
                long oldFrame = timeline.PlayHeadPosition;
                long newFrame = Math.Max(oldFrame + change, 0);
                if (newFrame != oldFrame) {
                    timeline.PlayHeadPosition = newFrame;
                }
            }
        }

        private void UpdatePixel(long frame, double zoom) {
            Thickness m = this.Margin;
            this.Margin = new Thickness(frame * zoom, m.Top, m.Right, m.Bottom);
        }

        private void OnPlayHeadFrameChanged() {
            if (this.Timeline is Timeline timeline)
                this.UpdatePixel(timeline.PlayHeadPosition, timeline.Zoom);
        }

        private static object OnCoerceFrameValue(DependencyObject d, object value) {
            return !(value is long frame) || frame < 0 ? 0L : value;
        }
    }
}