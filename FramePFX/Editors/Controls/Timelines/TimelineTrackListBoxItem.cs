using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItem : ListBoxItem {
        public Track Track { get; }

        public TimelineTrackListBox TrackList { get; set; }

        public TimelineTrackListBoxItem(Track track) {
            this.Track = track;
            this.Content = new TimelineTrackListBoxItemContent(this);
        }

        static TimelineTrackListBoxItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (TimelineTrackListBoxItem), new FrameworkPropertyMetadata(typeof(TimelineTrackListBoxItem)));
        }

        private void OnTrackHeightChanged(Track track) {
            this.Height = track.Height;
        }

        #region Model Linkage

        public void OnBeingAddedToTimeline() {
            this.Track.HeightChanged += this.OnTrackHeightChanged;
            ((TimelineTrackListBoxItemContent) this.Content).OnBeingAddedToTimeline();
        }

        public void OnAddedToTimeline() {
            this.Height = this.Track.Height;
            ((TimelineTrackListBoxItemContent) this.Content).OnAddedToTimeline();
        }

        public void OnBeginRemovedFromTimeline() {
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
            ((TimelineTrackListBoxItemContent) this.Content).OnBeginRemovedFromTimeline();
        }

        public void OnRemovedFromTimeline() {
            ((TimelineTrackListBoxItemContent) this.Content).OnRemovedFromTimeline();
        }

        public void OnIndexMoving(int oldIndex, int newIndex) {
        }

        public void OnIndexMoved(int oldIndex, int newIndex) {
        }

        #endregion
    }
}