using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Timelines;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls {
    public class PlayHeadControl : Control {
        private static readonly FieldInfo IsDraggingPropertyKeyField = typeof(Thumb).GetField("IsDraggingPropertyKey", BindingFlags.NonPublic | BindingFlags.Static);
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(PlayHeadControl), new PropertyMetadata(null, (d, e) => ((PlayHeadControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        private Thumb PART_ThumbHead;
        private Thumb PART_ThumbBody;
        private bool isDraggingThumb;

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
            this.UpdatePixel(newvalue, timeline.Zoom);
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

            long change = (long) (e.HorizontalChange / timeline.Zoom);
            if (change != 0) {
                long oldFrame = timeline.PlayHeadPosition;
                long newFrame = Math.Max(oldFrame + change, 0);
                if (newFrame >= timeline.TotalFrames) {
                    newFrame = timeline.TotalFrames - 1;
                }

                if (newFrame != oldFrame) {
                    timeline.PlayHeadPosition = newFrame;
                }
            }
        }

        public void EnableDragging(Point point) {
            this.isDraggingThumb = true;
            Thumb thumb = this.PART_ThumbBody ?? this.PART_ThumbHead;
            if (thumb == null) {
                return;
            }

            thumb.Focus();
            thumb.CaptureMouse();
            // lazy... could create custom control extending Thumb to modify this but this works so :D
            thumb.SetValue((DependencyPropertyKey) IsDraggingPropertyKeyField.GetValue(null), true);
            bool flag = true;
            try {
                thumb.RaiseEvent(new DragStartedEventArgs(point.X, point.Y));
                flag = false;
            }
            finally {
                if (flag) {
                    thumb.CancelDrag();
                }

                this.isDraggingThumb = false;
            }
        }

        private void UpdatePixel(long frame, double zoom) {
            Thickness m = this.Margin;
            this.Margin = new Thickness(frame * zoom, m.Top, m.Right, m.Bottom);
        }

        protected override Size MeasureOverride(Size constraint) {
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds) {
            return base.ArrangeOverride(arrangeBounds);
        }
    }
}