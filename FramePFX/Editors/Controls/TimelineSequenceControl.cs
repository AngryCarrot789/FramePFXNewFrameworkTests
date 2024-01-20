using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;
using FramePFX.WPF.Utils;

namespace FramePFX.Editors.Controls {
    /// <summary>
    /// A stack panel based control, that stacks a collection of tracks on top of each other, with a 1 pixel gap between each track
    /// </summary>
    public class TimelineSequenceControl : StackPanel {
        public const double MinZoom = 0.1D;
        public const double MaxZoom = 200D;

        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register(
                "Timeline",
                typeof(Timeline),
                typeof(TimelineSequenceControl),
                new PropertyMetadata(null, (d, e) => ((TimelineSequenceControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register(
                "ScrollViewer",
                typeof(ScrollViewer),
                typeof(TimelineSequenceControl),
                new FrameworkPropertyMetadata(
                    null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private static readonly DependencyPropertyKey UnitZoomPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "UnitZoom",
                typeof(double),
                typeof(TimelineSequenceControl),
                new PropertyMetadata(
                    1.0, (d, e) => ((TimelineSequenceControl) d).OnZoomChanged((double) e.NewValue), (d, value) => {
                        if (!(value is double zoom))
                            return 1.0d;
                        if (zoom < MinZoom)
                            return MinZoom;
                        return zoom > MaxZoom ? MaxZoom : value;
                    }));

        public static readonly DependencyProperty UnitZoomProperty = UnitZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// The model used to present the tracks, clips, etc. Event handlers will be added and removed when necessary
        /// </summary>
        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        /// <summary>
        /// A reference to the scroll viewer that this timeline sequence is placed in. This is required for zooming and scrolling
        /// </summary>
        public ScrollViewer ScrollViewer {
            get => (ScrollViewer) this.GetValue(ScrollViewerProperty);
            set => this.SetValue(ScrollViewerProperty, value);
        }

        public double UnitZoom {
            get => (double) this.GetValue(UnitZoomProperty);
            private set => this.SetValue(UnitZoomPropertyKey, value);
        }

        public double TotalFramePixels => this.UnitZoom * this.Timeline.TotalFrames;

        public TimelineSequenceControl() {
        }

        static TimelineSequenceControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineSequenceControl), new FrameworkPropertyMetadata(typeof(TimelineSequenceControl)));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e) {
            base.OnMouseWheel(e);
            if (e.Handled) {
                return;
            }

            ScrollViewer scroller = this.ScrollViewer;
            if (scroller == null) {
                return;
            }

            Timeline timeline = this.Timeline;
            if (timeline == null) {
                return;
            }

            ModifierKeys mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Alt) != 0) {
                if (VisualTreeUtils.GetParent<TimelineTrackControl>(e.OriginalSource as DependencyObject) is TimelineTrackControl track) {
                    track.Track.Height = Maths.Clamp(track.Track.Height + (e.Delta / 120d) * 20d, TimelineClipControl.HeaderSize, 200d);
                }

                e.Handled = true;
            }
            else if ((mods & ModifierKeys.Control) != 0) {
                e.Handled = true;
                bool shift = (mods & ModifierKeys.Shift) != 0;
                double multiplier = (shift ? 0.2 : 0.4);
                if (e.Delta > 0) {
                    multiplier = 1d + multiplier;
                }
                else {
                    multiplier = 1d - multiplier;
                }

                double oldzoom = timeline.Zoom;
                double newzoom = Math.Max(oldzoom * multiplier, 0.0001d);
                double minzoom = scroller.ViewportWidth / (scroller.ExtentWidth / oldzoom); // add 0.000000000000001 to never disable scroll bar
                newzoom = Math.Max(minzoom, newzoom);
                timeline.SetZoom(newzoom, ZoomType.Direct); // let the coerce function clamp the zoom value
                newzoom = timeline.Zoom;

                // managed to get zooming towards the cursor working
                double mouse_x = e.GetPosition(scroller).X;
                double target_offset = (scroller.HorizontalOffset + mouse_x) / oldzoom;
                double scaled_target_offset = target_offset * newzoom;
                double new_offset = scaled_target_offset - mouse_x;
                scroller.ScrollToHorizontalOffset(new_offset);
            }
            else if ((mods & ModifierKeys.Shift) != 0) {
                if (e.Delta < 0) {
                    for (int i = 0; i < 6; i++) {
                        scroller.LineRight();
                    }
                }
                else {
                    for (int i = 0; i < 6; i++) {
                        scroller.LineLeft();
                    }
                }
                e.Handled = true;
            }
        }

        private void UpdateTotalFramesUsage() {
            this.InvalidateMeasure();
        }

        private void OnZoomChanged(double newZoom) {
            foreach (TimelineTrackControl track in this.InternalChildren) {
                track.OnZoomChanged(newZoom);
            }
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline == newTimeline)
                return;
            if (oldTimeline != null) {
                oldTimeline.TrackAdded -= this.OnTrackAdded;
                oldTimeline.TrackRemoved -= this.OnTrackRemoved;
                oldTimeline.TrackMoved -= this.OnTrackMoved;
                oldTimeline.TotalFramesChanged -= this.OnTotalFramesChanged;
                oldTimeline.ZoomTimeline -= this.OnTimelineZoomed;
                for (int i = this.InternalChildren.Count - 1; i >= 0; i--) {
                    this.RemoveTrackInternal(oldTimeline.Tracks[i], i);
                }
            }

            if (newTimeline != null) {
                newTimeline.TrackAdded += this.OnTrackAdded;
                newTimeline.TrackRemoved += this.OnTrackRemoved;
                newTimeline.TrackMoved += this.OnTrackMoved;
                newTimeline.TotalFramesChanged += this.OnTotalFramesChanged;
                newTimeline.ZoomTimeline += this.OnTimelineZoomed;
                int i = 0;
                foreach (Track track in newTimeline.Tracks) {
                    this.InsertTrackInternal(track, i++);
                }

                this.UnitZoom = newTimeline.Zoom;
            }
        }

        private void OnTimelineZoomed(Timeline timeline, double oldzoom, double newzoom, ZoomType zoomtype) {
            this.UnitZoom = newzoom;
            ScrollViewer scroller = this.ScrollViewer;
            if (scroller == null) {
                return;
            }

            switch (zoomtype) {
                case ZoomType.Direct: break;
                case ZoomType.ViewPortBegin: {
                    break;
                }
                case ZoomType.ViewPortMiddle: {
                    break;
                }
                case ZoomType.ViewPortEnd: {
                    break;
                }
                case ZoomType.PlayHead: {
                    break;
                }
                case ZoomType.MouseCursor: {
                    double mouse_x = Mouse.GetPosition(scroller).X;
                    double target_offset = (scroller.HorizontalOffset + mouse_x) / oldzoom;
                    double scaled_target_offset = target_offset * newzoom;
                    double new_offset = scaled_target_offset - mouse_x;
                    scroller.ScrollToHorizontalOffset(new_offset);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(zoomtype), zoomtype, null);
            }
        }

        private void OnTotalFramesChanged(Timeline timeline) => this.UpdateTotalFramesUsage();

        private void OnTrackAdded(Timeline timeline, Track track, int index) {
            this.InsertTrackInternal(track, index);
        }

        private void OnTrackRemoved(Timeline timeline, Track track, int index) {
            this.RemoveTrackInternal(track, index);
        }

        private void OnTrackMoved(Timeline timeline, Track track, int oldIndex, int newIndex) {
            TimelineTrackControl control = (TimelineTrackControl) this.InternalChildren[oldIndex];
            control.OnIndexMoving(oldIndex, newIndex);
            this.InternalChildren.RemoveAt(oldIndex);
            this.InternalChildren.Insert(newIndex, control);
            control.OnIndexMoved(oldIndex, newIndex);
            this.InvalidateMeasure();
        }

        private void InsertTrackInternal(Track track, int index) {
            TimelineTrackControl control = new TimelineTrackControl(track);
            control.Timeline = this;
            control.OnBeingAddedToTimeline();
            this.InternalChildren.Insert(index, control);
            control.OnAddedToTimeline();
            control.InvalidateMeasure();
            this.InvalidateMeasure();
            this.InvalidateVisual();
        }

        private void RemoveTrackInternal(Track track, int index) {
            TimelineTrackControl control = (TimelineTrackControl) this.InternalChildren[index];
            control.OnBeginRemovedFromTimeline();
            this.InternalChildren.RemoveAt(index);
            control.OnRemovedFromTimeline();
            control.Timeline = null;
            this.InvalidateMeasure();
            this.InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize) {
            double totalHeight = 0d;
            double maxWidth = 0d;
            UIElementCollection items = this.InternalChildren;
            int count = items.Count;
            for (int i = 0; i < count; i++) {
                TimelineTrackControl track = (TimelineTrackControl) items[i];
                track.Measure(availableSize);
                totalHeight += track.DesiredSize.Height;
                maxWidth = Math.Max(maxWidth, track.RenderSize.Width);
            }

            double width = this.Timeline != null ? this.TotalFramePixels : maxWidth;
            if (count > 1) {
                totalHeight += (count - 1);
            }

            return new Size(width, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            double totalY = 0d;
            UIElementCollection items = this.InternalChildren;
            for (int i = 0, count = items.Count; i < count; i++) {
                TimelineTrackControl track = (TimelineTrackControl) items[i];
                track.Arrange(new Rect(new Point(0, totalY), new Size(finalSize.Width, track.DesiredSize.Height)));
                totalY += track.RenderSize.Height + 1d;
            }

            return finalSize;
        }

        public TimelineTrackControl GetTrackByModel(Track track) {
            UIElementCollection list = this.InternalChildren;
            for (int i = 0, count = list.Count; i < count; i++) {
                TimelineTrackControl control = (TimelineTrackControl) list[i];
                if (control.Track == track) {
                    return control;
                }
            }

            return null;
        }
    }
}
