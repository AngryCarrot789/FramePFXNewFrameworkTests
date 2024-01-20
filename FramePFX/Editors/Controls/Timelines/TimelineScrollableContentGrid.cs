using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Timelines;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineScrollableContentGrid : Grid {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineScrollableContentGrid), new PropertyMetadata(null, OnTimelineChanged));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public TimelineControl TimelineControl { get; set; }

        public TimelineScrollableContentGrid() {
        }

        protected override void OnMouseDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);
            if (!e.Handled && this.TimelineControl != null) {
                Point point = e.GetPosition(this);
                this.TimelineControl.SetPlayHeadToMouseCursor(point.X);
            }
        }

        private static void OnTimelineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TimelineScrollableContentGrid grid = (TimelineScrollableContentGrid) d;
            if (e.OldValue is Timeline oldTimeline) {
                oldTimeline.PlayHeadChanged -= grid.OnPlayHeadChanged;
                oldTimeline.ZoomTimeline -= grid.OnTimelineZoomed;
            }

            if (e.NewValue is Timeline newTimeline) {
                newTimeline.PlayHeadChanged += grid.OnPlayHeadChanged;
                newTimeline.ZoomTimeline += grid.OnTimelineZoomed;
            }
        }

        private void OnPlayHeadChanged(Timeline timeline, long oldvalue, long newvalue) {
            this.InvalidateMeasure();
        }

        private void OnTimelineZoomed(Timeline timeline, double oldzoom, double newzoom, ZoomType zoomtype) {
            this.InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size constraint) {
            if (this.Timeline is Timeline timeline) {
                constraint = new Size(timeline.TotalFrames * timeline.Zoom, constraint.Height);
            }

            Size measure = base.MeasureOverride(constraint);
            return measure;
        }

        protected override Size ArrangeOverride(Size arrangeSize) {
            return base.ArrangeOverride(arrangeSize);
        }
    }
}
