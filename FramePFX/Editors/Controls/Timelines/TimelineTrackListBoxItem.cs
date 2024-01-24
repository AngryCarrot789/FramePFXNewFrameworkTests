using System;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItem : ListBoxItem {
        /// <summary>
        /// Gets this track item's associated track model
        /// </summary>
        public Track Track { get; private set; }

        /// <summary>
        /// Gets our owner list
        /// </summary>
        public TimelineTrackListBox TrackList { get; private set; }

        private readonly GetSetAutoPropertyBinder<Track> isSelectedBinder = new GetSetAutoPropertyBinder<Track>(IsSelectedProperty, nameof(VideoTrack.IsSelectedChanged), b => b.Model.IsSelected.Box(), (b, v) => b.Model.IsSelected = (bool) v);

        public TimelineTrackListBoxItem() {
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

        public void OnAddingToList(TimelineTrackListBox ownerList, Track track) {
            this.Track = track ?? throw new ArgumentNullException(nameof(track));
            this.TrackList = ownerList;
            this.Track.HeightChanged += this.OnTrackHeightChanged;
            this.Content = ownerList.GetContentObject(track.GetType());
        }

        public void OnAddedToList() {
            ((TimelineTrackListBoxItemContent) this.Content).Connect(this);
            this.Height = this.Track.Height;
            this.isSelectedBinder.Attach(this, this.Track);
        }

        public void OnRemovingFromList() {
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
            this.isSelectedBinder.Detatch();
            TimelineTrackListBoxItemContent content = (TimelineTrackListBoxItemContent) this.Content;
            content.Disconnect();
            this.Content = null;
            this.TrackList.ReleaseContentObject(this.Track.GetType(), content);
        }

        public void OnRemovedFromList() {
            this.TrackList = null;
            this.Track = null;
        }

        public void OnIndexMoving(int oldIndex, int newIndex) {
        }

        public void OnIndexMoved(int oldIndex, int newIndex) {
        }

        #endregion
    }
}