using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FramePFX.Destroying;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;

namespace FramePFX.Editors.Timelines {
    public delegate void TimelineTrackIndexEventHandler(Timeline timeline, Track track, int index);
    public delegate void TimelineTrackMovedEventHandler(Timeline timeline, Track track, int oldIndex, int newIndex);
    public delegate void TimelineEventHandler(Timeline timeline);

    public class Timeline : IDestroy {
        private readonly List<Track> tracks;
        private long totalFrames;

        public event TimelineTrackIndexEventHandler TrackAdded;
        public event TimelineTrackIndexEventHandler TrackRemoved;
        public event TimelineTrackMovedEventHandler TrackMoved;
        public event TimelineEventHandler TotalFramesChanged;

        public Project Project { get; private set; }

        public ReadOnlyCollection<Track> Tracks { get; }


        /// <summary>
        /// Gets or sets the total length of all tracks, in frames. This is incremented on demand when necessary, and is used for UI calculations
        /// </summary>
        public long TotalFrames {
            get => this.totalFrames;
            set {
                if (this.totalFrames == value)
                    return;
                this.totalFrames = value;
                this.TotalFramesChanged?.Invoke(this);
            }
        }

        public Timeline() {
            this.tracks = new List<Track>();
            this.Tracks = new ReadOnlyCollection<Track>(this.tracks);
            this.totalFrames = 1000L;
        }

        public void AddTrack(Track clip) => this.InsertTrack(this.tracks.Count, clip);

        public void InsertTrack(int index, Track track) {
            if (this.tracks.Contains(track))
                throw new InvalidOperationException("This track already contains the track");
            this.tracks.Insert(index, track);
            Track.OnAddedToTimeline(track, this);
            this.TrackAdded?.Invoke(this, track, index);
        }

        public bool RemoveTrack(Track track) {
            int index = this.tracks.IndexOf(track);
            if (index == -1)
                return false;
            this.RemoveTrackAt(index);
            return true;
        }

        public void RemoveTrackAt(int index) {
            Track track = this.tracks[index];
            this.tracks.RemoveAt(index);
            Track.OnRemovedFromTimeline(track, this);
            this.TrackRemoved?.Invoke(this, track, index);
        }

        public void MoveTrackIndex(int oldIndex, int newIndex) {
            if (oldIndex != newIndex) {
                this.tracks.MoveItem(oldIndex, newIndex);
                this.TrackMoved?.Invoke(this, this.tracks[newIndex], oldIndex, newIndex);
            }
        }

        public virtual void Destroy() {
            for (int i = this.tracks.Count - 1; i >= 0; i--) {
                Track track = this.tracks[i];
                track.Destroy();
                this.RemoveTrackAt(i);
            }
        }

        public static void SetMainTimelineProjectReference(Timeline timeline, Project project) {
            // no need to tell clips or tracks that our project changed, since there is guaranteed
            // to be none, unless this method is called outside of the project's constructor
            timeline.Project = project;
        }

        // TODO: composition timelines
        public static void SetCompositionTimelineProjectReference(Timeline timeline, Project project) {

        }
    }
}