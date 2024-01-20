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
    public delegate void PlayheadChangedEventHandler(Timeline timeline, long oldValue, long newValue);
    public delegate void ZoomEventHandler(Timeline timeline, double oldZoom, double newZoom, ZoomType zoomType);

    public class Timeline : IDestroy {
        private readonly List<Track> tracks;
        private long totalFrames;
        private long playHead;
        private double zoom;

        public event TimelineTrackIndexEventHandler TrackAdded;
        public event TimelineTrackIndexEventHandler TrackRemoved;
        public event TimelineTrackMovedEventHandler TrackMoved;
        public event TimelineEventHandler TotalFramesChanged;
        public event PlayheadChangedEventHandler PlayHeadChanged;
        public event ZoomEventHandler ZoomTimeline;

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

        /// <summary>
        /// The position of the play head, in frames
        /// </summary>
        public long PlayHeadPosition {
            get => this.playHead;
            set {
                if (this.playHead == value)
                    return;

                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Playhead cannot be negative");
                if (value >= this.totalFrames)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Playhead exceeds the timeline duration range (0 to TotalFrames)");

                long oldPlayHead = this.playHead;
                this.playHead = value;
                this.PlayHeadChanged?.Invoke(this, oldPlayHead, value);
            }
        }

        public double Zoom => this.zoom;

        public Timeline() {
            this.tracks = new List<Track>();
            this.Tracks = new ReadOnlyCollection<Track>(this.tracks);
            this.totalFrames = 5000L;
            this.zoom = 1.0d;
        }

        public void SetZoom(double zoom, ZoomType type) {
            double oldZoom = this.zoom;
            if (zoom > 200.0) {
                zoom = 200;
            }
            else if (zoom < 0.1) {
                zoom = 0.1;
            }

            if (Maths.Equals(oldZoom, zoom)) {
                return;
            }

            this.zoom = zoom;
            this.ZoomTimeline?.Invoke(this, oldZoom, zoom, type);
        }

        public void AddTrack(Track track) => this.InsertTrack(this.tracks.Count, track);

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