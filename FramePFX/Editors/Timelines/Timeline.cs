using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FramePFX.Destroying;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Utils;

namespace FramePFX.Editors.Timelines {
    public delegate void TimelineTrackIndexEventHandler(Timeline timeline, Track track, int index);
    public delegate void TimelineTrackMovedEventHandler(Timeline timeline, Track track, int oldIndex, int newIndex);
    public delegate void TimelineEventHandler(Timeline timeline);
    public delegate void PlayheadChangedEventHandler(Timeline timeline, long oldValue, long newValue);
    public delegate void ZoomEventHandler(Timeline timeline, double oldZoom, double newZoom, ZoomType zoomType);

    public class Timeline : IDestroy {
        private readonly List<Track> tracks;
        private readonly List<Track> selection;
        private long totalFrames;
        private long playHead;
        private long largestFrameInUse;

        public event TimelineTrackIndexEventHandler TrackAdded;
        public event TimelineTrackIndexEventHandler TrackRemoved;
        public event TimelineTrackMovedEventHandler TrackMoved;
        public event TimelineEventHandler TotalFramesChanged;
        public event TimelineEventHandler LargestFrameInUseChanged;
        public event PlayheadChangedEventHandler PlayHeadChanged;
        public event ZoomEventHandler ZoomTimeline;

        public Project Project { get; private set; }

        public TrackFrameSpan RangedSelectionAnchor { get; set; } = TrackFrameSpan.Invalid;

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

        public long LargestFrameInUse {
            get => this.largestFrameInUse;
            private set {
                if (this.largestFrameInUse == value)
                    return;
                this.largestFrameInUse = value;
                this.LargestFrameInUseChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Returns an enumerable of selected tracks
        /// </summary>
        public IEnumerable<Track> SelectedTracks => this.selection;

        /// <summary>
        /// Returns an enumerable of all selected clips in all tracks
        /// </summary>
        public IEnumerable<Clip> SelectedClips => this.tracks.SelectMany(t => t.SelectedClips);

        /// <summary>
        /// Returns the track selection type based on how many tracks are selected.
        /// Does not require enumerating the tracks as track selection is cached
        /// </summary>
        public SelectionType TrackSelectionType {
            get {
                int count = this.selection.Count;
                if (count > 1)
                    return SelectionType.Multi;
                return count == 1 ? SelectionType.Single : SelectionType.None;
            }
        }

        /// <summary>
        /// Returns the clip selection type based on how many clips are selected in all tracks combined.
        /// This may require enumerating all tracks, but not all clips (since selected clips are cached)
        /// </summary>
        public SelectionType ClipSelectionType {
            get {
                int count = 0;
                foreach (Track track in this.tracks) {
                    count += track.SelectedClipCount;
                    if (count > 1) {
                        return SelectionType.Multi;
                    }
                }

                return count == 1 ? SelectionType.Single : SelectionType.None;
            }
        }

        /// <summary>
        /// Returns true when there is at least one selected clips in any track. This may
        /// require enumerating all tracks, but not all clips (since selected clips are cached)
        /// </summary>
        public bool HasAnySelectedClips => this.tracks.Any(track => track.SelectedClipCount > 0);

        public double Zoom { get; private set; }

        public PlaybackManager Playback { get; }

        public Timeline() {
            this.tracks = new List<Track>();
            this.Tracks = new ReadOnlyCollection<Track>(this.tracks);
            this.selection = new List<Track>();

            this.totalFrames = 5000L;
            this.Zoom = 1.0d;

            this.Playback = new PlaybackManager(this);
            this.Playback.SetFrameRate(60.0);
            this.Playback.StartTimer();
        }

        public void UpdateLargestFrame() {
            IReadOnlyList<Track> list = this.Tracks;
            int count = list.Count;
            if (count > 0) {
                long max = list[0].LargestFrameInUse;
                for (int i = 1; i < count; i++) {
                    max = Math.Max(max, list[i].LargestFrameInUse);
                }

                this.LargestFrameInUse = max;
            }
            else {
                this.LargestFrameInUse = 0;
            }
        }

        public void SetZoom(double zoom, ZoomType type) {
            double oldZoom = this.Zoom;
            if (zoom > 200.0) {
                zoom = 200;
            }
            else if (zoom < 0.1) {
                zoom = 0.1;
            }

            if (Maths.Equals(oldZoom, zoom)) {
                return;
            }

            this.Zoom = zoom;
            this.ZoomTimeline?.Invoke(this, oldZoom, zoom, type);
        }

        public void AddTrack(Track track) => this.InsertTrack(this.tracks.Count, track);

        public void InsertTrack(int index, Track track) {
            if (this.tracks.Contains(track))
                throw new InvalidOperationException("This track already contains the track");
            this.tracks.Insert(index, track);
            if (track.IsSelected)
                this.selection.Add(track);

            // update anchor
            TrackFrameSpan anchor = this.RangedSelectionAnchor;
            if (anchor.TrackIndex != -1) {
                if (index <= anchor.TrackIndex) {
                    this.RangedSelectionAnchor = new TrackFrameSpan(anchor.Span, anchor.TrackIndex + 1);
                }
            }

            Track.OnAddedToTimeline(track, this);
            this.TrackAdded?.Invoke(this, track, index);
            this.UpdateLargestFrame();
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
            if (track.IsSelected)
                this.selection.Remove(track);

            // update anchor
            TrackFrameSpan anchor = this.RangedSelectionAnchor;
            if (anchor.TrackIndex != -1) {
                if (this.tracks.Count == 0) {
                    this.RangedSelectionAnchor = TrackFrameSpan.Invalid;
                }
                else if (index <= anchor.TrackIndex) {
                    this.RangedSelectionAnchor = new TrackFrameSpan(anchor.Span, anchor.TrackIndex - 1);
                }
            }

            Track.OnRemovedFromTimeline(track, this);
            this.TrackRemoved?.Invoke(this, track, index);
            this.UpdateLargestFrame();
        }

        public void MoveTrackIndex(int oldIndex, int newIndex) {
            if (oldIndex != newIndex) {
                this.tracks.MoveItem(oldIndex, newIndex);

                // update anchor
                TrackFrameSpan anchor = this.RangedSelectionAnchor;
                if (anchor.TrackIndex != -1 && anchor.TrackIndex == oldIndex) {
                    this.RangedSelectionAnchor = new TrackFrameSpan(anchor.Span, newIndex);
                }

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

        // Called by the track directly, in order to guarantee that selection is
        // handled before any track IsSelectedChanged event handlers
        public static void OnIsTrackSelectedChanged(Track track) {
            if (track.IsSelected) {
                track.Timeline.selection.Add(track);
            }
            else {
                track.Timeline.selection.Remove(track);
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

        public void ClearClipSelection() {
            List<Clip> list = this.SelectedClips.ToList();
            foreach (Clip clip in list) {
                clip.IsSelected = false;
            }
        }

        public void MakeSingleSelection(Clip clipToSelect, bool updateAnchor = true) {
            this.ClearClipSelection();
            clipToSelect.IsSelected = true;
            if (updateAnchor) {
                this.RangedSelectionAnchor = new TrackFrameSpan(clipToSelect);
            }
        }

        public void MakeFrameRangeSelection(FrameSpan span, int trackSrcIdx = -1, int trackEndIndex = -1) {
            this.ClearClipSelection();
            List<Clip> clips = new List<Clip>();
            if (trackSrcIdx == -1 || trackEndIndex == -1) {
                foreach (Track track in this.tracks) {
                    track.CollectClipsInSpan(clips, span);
                }
            }
            else {
                for (int i = trackSrcIdx; i < trackEndIndex; i++) {
                    this.tracks[i].CollectClipsInSpan(clips, span);
                }
            }

            foreach (Clip clip in clips) {
                clip.IsSelected = true;
            }
        }

        internal static void OnIsClipSelectedChanged(Track track, Clip clip) {
            // track.Timeline.RangedSelectionAnchorClip = clip.IsSelected ? clip : null;
        }

        internal static void OnClipRemovedFromTrack(Track track, Clip clip) {
            // if (track.Timeline.RangedSelectionAnchorClip == clip) {
            //     track.Timeline.RangedSelectionAnchorClip = null;
            // }
        }
    }
}