using System;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItem : ListBoxItem {
        public Track Track { get; }

        public TimelineTrackListBox TrackList { get; set; }

        private readonly GetSetAutoPropertyBinder<Track> isSelectedBinder = new GetSetAutoPropertyBinder<Track>(IsSelectedProperty, nameof(VideoTrack.IsSelectedChanged), b => b.Model.IsSelected.Box(), (b, v) => b.Model.IsSelected = (bool) v);

        public TimelineTrackListBoxItem(Track track) {
            this.Track = track;
            if (track is VideoTrack) {
                this.Content = new TimelineTrackListBoxItemContent_Video(this);
            }
            else {
                throw new Exception("Unsupported track type: " + track?.GetType().Name);
            }
        }

        static TimelineTrackListBoxItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (TimelineTrackListBoxItem), new FrameworkPropertyMetadata(typeof(TimelineTrackListBoxItem)));
        }

        private void OnTrackHeightChanged(Track track) {
            this.Height = track.Height;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            this.isSelectedBinder?.OnPropertyChanged(e);
        }

        #region Model Linkage

        public void OnAddingToTimeline() {
            this.Track.HeightChanged += this.OnTrackHeightChanged;
            ((TimelineTrackListBoxItemContent) this.Content).OnBeingAddedToTimeline();
        }

        public void OnAddedToTimeline() {
            this.Height = this.Track.Height;
            ((TimelineTrackListBoxItemContent) this.Content).OnAddedToTimeline();
            this.isSelectedBinder.Attach(this, this.Track);
        }

        public void OnRemovingFromTimeline() {
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
            ((TimelineTrackListBoxItemContent) this.Content).OnBeginRemovedFromTimeline();
        }

        public void OnRemovedFromTimeline() {
            ((TimelineTrackListBoxItemContent) this.Content).OnRemovedFromTimeline();
            this.isSelectedBinder.Detatch();
        }

        public void OnIndexMoving(int oldIndex, int newIndex) {
        }

        public void OnIndexMoved(int oldIndex, int newIndex) {
        }

        #endregion
    }
}