using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls {
    public class TimelineControl : StackPanel {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineControl), new PropertyMetadata(null, (d, e) => ((TimelineControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));
        public static readonly DependencyProperty ScrollViewerProperty = DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(TimelineControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public ScrollViewer ScrollViewer {
            get => (ScrollViewer) this.GetValue(ScrollViewerProperty);
            set => this.SetValue(ScrollViewerProperty, value);
        }

        public double HorizontalScrollOffset { get; set; }

        public double UnitZoom { get; set; }

        public double TotalFramePixels => this.UnitZoom * this.Timeline.TotalFrames;

        public TimelineControl() {
            this.UnitZoom = 1;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.UseLayoutRounding = true;
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

            ModifierKeys mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Alt) != 0) {
                if (e.OriginalSource is TimelineTrackControl track) {
                    track.Height = Maths.Clamp(track.Height + (e.Delta / 120d) * 20d, track.MinHeight, track.MaxHeight);
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

                double oldzoom = this.UnitZoom;
                double newzoom = Math.Max(oldzoom * multiplier, 0.0001d);
                double minzoom = scroller.ViewportWidth / (scroller.ExtentWidth / oldzoom); // add 0.000000000000001 to never disable scroll bar
                newzoom = Math.Max(minzoom, newzoom);
                this.SetZoom(newzoom); // let the coerce function clamp the zoom value
                newzoom = this.UnitZoom;

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

        public void SetZoom(double newZoom) {
            if (Math.Abs(this.UnitZoom - newZoom) < 0.00001d)
                return;
            this.UnitZoom = newZoom;
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
                for (int i = this.InternalChildren.Count - 1; i >= 0; i--) {
                    this.RemoveTrackInternal(oldTimeline.Tracks[i], i);
                }
            }

            if (newTimeline != null) {
                newTimeline.TrackAdded += this.OnTrackAdded;
                newTimeline.TrackRemoved += this.OnTrackRemoved;
                newTimeline.TrackMoved += this.OnTrackMoved;
                newTimeline.TotalFramesChanged += this.OnTotalFramesChanged;

                int i = 0;
                foreach (Track track in newTimeline.Tracks) {
                    this.InsertTrackInternal(track, i++);
                }
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
        }

        private void RemoveTrackInternal(Track track, int index) {
            TimelineTrackControl control = (TimelineTrackControl) this.InternalChildren[index];
            control.OnBeginRemovedFromTimeline();
            this.InternalChildren.RemoveAt(index);
            control.OnRemovedFromTimeline();
            control.Timeline = null;
            this.InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize) {
            double totalHeight = 1d;
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

            return new Size(width + 2d, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            double totalY = 1d;
            UIElementCollection items = this.InternalChildren;
            for (int i = 0, count = items.Count; i < count; i++) {
                TimelineTrackControl track = (TimelineTrackControl) items[i];
                track.Arrange(new Rect(new Point(1d, totalY), new Size(finalSize.Width - 2d, track.DesiredSize.Height)));
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
