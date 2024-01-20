using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBox : ListBox {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineTrackListBox), new PropertyMetadata(null, (d, e) => ((TimelineTrackListBox) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public TimelineTrackListBox() {
            this.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(TimelineTrackListBoxItemPanel)));
        }

        static TimelineTrackListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (TimelineTrackListBox), new FrameworkPropertyMetadata(typeof(TimelineTrackListBox)));
        }

        protected override Size MeasureOverride(Size constraint) {
            return base.MeasureOverride(constraint);
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline == newTimeline)
                return;
            if (oldTimeline != null) {
                oldTimeline.TrackAdded -= this.OnTrackAdded;
                oldTimeline.TrackRemoved -= this.OnTrackRemoved;
                oldTimeline.TrackMoved -= this.OnTrackMoved;
                for (int i = this.Items.Count - 1; i >= 0; i--) {
                    this.RemoveTrackInternal(oldTimeline.Tracks[i], i);
                }
            }

            if (newTimeline != null) {
                newTimeline.TrackAdded += this.OnTrackAdded;
                newTimeline.TrackRemoved += this.OnTrackRemoved;
                newTimeline.TrackMoved += this.OnTrackMoved;

                int i = 0;
                foreach (Track track in newTimeline.Tracks) {
                    this.InsertTrackInternal(track, i++);
                }
            }
        }

        private void OnTrackAdded(Timeline timeline, Track track, int index) {
            this.InsertTrackInternal(track, index);
        }

        private void OnTrackRemoved(Timeline timeline, Track track, int index) {
            this.RemoveTrackInternal(track, index);
        }

        private void InsertTrackInternal(Track track, int index) {
            TimelineTrackListBoxItem control = new TimelineTrackListBoxItem(track);
            control.TrackList = this;
            control.OnBeingAddedToTimeline();
            this.Items.Insert(index, control);
            control.OnAddedToTimeline();
            control.InvalidateMeasure();
            this.InvalidateMeasure();
        }

        private void RemoveTrackInternal(Track track, int index) {
            TimelineTrackListBoxItem control = (TimelineTrackListBoxItem) this.Items[index];
            control.OnBeginRemovedFromTimeline();
            this.Items.RemoveAt(index);
            control.OnRemovedFromTimeline();
            control.TrackList = null;
            this.InvalidateMeasure();
        }

        private void OnTrackMoved(Timeline timeline, Track track, int oldIndex, int newIndex) {
            TimelineTrackListBoxItem control = (TimelineTrackListBoxItem) this.Items[oldIndex];
            control.OnIndexMoving(oldIndex, newIndex);
            this.Items.RemoveAt(oldIndex);
            this.Items.Insert(newIndex, control);
            control.OnIndexMoved(oldIndex, newIndex);
            this.InvalidateMeasure();
        }
    }
}