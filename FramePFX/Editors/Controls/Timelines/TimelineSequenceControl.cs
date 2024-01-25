using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    /// <summary>
    /// A stack panel based control, that stacks a collection of tracks on top of each other, with a 1 pixel gap between
    /// each track. This is what presents the actual timeline, track and clip data
    /// </summary>
    public class TimelineSequenceControl : StackPanel {
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

        /// <summary>
        /// Gets or sets the host timeline control that this sequence can access
        /// </summary>
        public TimelineControl TimelineControl { get; set; }

        public double TotalFramePixels => this.Timeline.Zoom * this.Timeline.MaxDuration;

        private const int MaxCachedTracks = 4;
        private readonly Stack<TimelineTrackControl> cachedTracks;

        public TimelineSequenceControl() {
            this.cachedTracks = new Stack<TimelineTrackControl>();
        }

        public void SetPlayHeadToMouseCursor(MouseDevice device) {
            if (this.TimelineControl != null) {
                Point point = device.GetPosition(this);
                this.TimelineControl.SetPlayHeadToMouseCursor(point.X);
            }
        }

        static TimelineSequenceControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineSequenceControl), new FrameworkPropertyMetadata(typeof(TimelineSequenceControl)));
        }

        private void UpdateTotalFramesUsage() {
            this.InvalidateMeasure();
        }

        public void OnZoomChanged(double newZoom) {
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
                oldTimeline.MaxDurationChanged -= this.OnMaxDurationChanged;
                for (int i = this.InternalChildren.Count - 1; i >= 0; i--) {
                    this.RemoveTrackInternal(i);
                }
            }

            if (newTimeline != null) {
                newTimeline.TrackAdded += this.OnTrackAdded;
                newTimeline.TrackRemoved += this.OnTrackRemoved;
                newTimeline.TrackMoved += this.OnTrackMoved;
                newTimeline.MaxDurationChanged += this.OnMaxDurationChanged;
                int i = 0;
                foreach (Track track in newTimeline.Tracks) {
                    this.InsertTrackInternal(track, i++);
                }
            }
        }

        private void OnMaxDurationChanged(Timeline timeline) => this.UpdateTotalFramesUsage();

        private void OnTrackAdded(Timeline timeline, Track track, int index) {
            this.InsertTrackInternal(track, index);
        }

        private void OnTrackRemoved(Timeline timeline, Track track, int index) {
            this.RemoveTrackInternal(index);
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
            TimelineTrackControl control = this.cachedTracks.Count > 0 ? this.cachedTracks.Pop() : new TimelineTrackControl();
            control.Timeline = this;
            control.OnBeingAddedToTimeline(this, track);
            this.InternalChildren.Insert(index, control);
            control.OnAddedToTimeline();
            control.InvalidateMeasure();
            this.InvalidateMeasure();
            this.InvalidateVisual();
        }

        private void RemoveTrackInternal(int index) {
            TimelineTrackControl control = (TimelineTrackControl) this.InternalChildren[index];
            control.OnBeginRemovedFromTimeline();
            this.InternalChildren.RemoveAt(index);
            control.OnRemovedFromTimeline();
            if (this.cachedTracks.Count < MaxCachedTracks)
                this.cachedTracks.Push(control);
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
