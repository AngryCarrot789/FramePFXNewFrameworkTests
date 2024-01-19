using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls {
    public class TimelineTrackListBoxItem : ListBoxItem {
        public Track Track { get; }

        public TimelineTrackListBox TrackList { get; set; }

        public TimelineTrackListBoxItem(Track track) {
            this.Track = track;
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
        }

        public void OnAddedToTimeline() {
            this.Height = this.Track.Height;
        }

        public void OnBeginRemovedFromTimeline() {
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
        }

        public void OnRemovedFromTimeline() {
        }

        public void OnIndexMoving(int oldIndex, int newIndex) {
        }

        public void OnIndexMoved(int oldIndex, int newIndex) {
        }

        #endregion
    }
}