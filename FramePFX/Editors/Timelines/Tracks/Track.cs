using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FramePFX.Destroying;
using FramePFX.Editors.Factories;
using FramePFX.Editors.Timelines.Tracks.Clips;
using SkiaSharp;

namespace FramePFX.Editors.Timelines.Tracks {
    public delegate void TrackEventHandler(Track track);
    public delegate void TrackClipIndexEventHandler(Track track, Clip clip, int index);
    public delegate void ClipMovedEventHandler(Clip clip, Track oldTrack, int oldIndex, Track newTrack, int newIndex);

    public abstract class Track : IDestroy {
        public string FactoryId => TrackFactory.Instance.GetId(this.GetType());

        public Timeline Timeline { get; private set; }

        public ReadOnlyCollection<Clip> Clips { get; }

        public double Height {
            get => this.height;
            set {
                this.height = value;
                this.HeightChanged?.Invoke(this);
            }
        }

        public string DisplayName {
            get => this.displayName;
            set {
                if (this.displayName == value)
                    return;
                this.displayName = value;
                this.DisplayNameChanged?.Invoke(this);
            }
        }

        public SKColor Colour {
            get => this.colour;
            set {
                if (this.colour == value)
                    return;
                this.colour = value;
                this.ColourChanged?.Invoke(this);
            }
        }

        public bool IsSelected {
            get => this.isSelected;
            set {
                if (this.isSelected == value)
                    return;
                this.isSelected = value;
                Timeline.OnIsTrackSelectedChanged(this);
                this.IsSelectedChanged?.Invoke(this);
            }
        }

        public long LargestFrameInUse => this.cache.LargestActiveFrame;

        public IEnumerable<Clip> SelectedClips => this.selectedClips;

        public int SelectedClipCount => this.selectedClips.Count;

        /// <summary>
        /// Gets the index of this track within our owner timeline
        /// </summary>
        public int IndexInTimeline => this.Timeline?.Tracks.IndexOf(this) ?? -1;

        public event TrackClipIndexEventHandler ClipAdded;
        public event TrackClipIndexEventHandler ClipRemoved;
        public event ClipMovedEventHandler ClipMovedTracks;
        public event TrackEventHandler HeightChanged;
        public event TrackEventHandler DisplayNameChanged;
        public event TrackEventHandler ColourChanged;
        public event TrackEventHandler IsSelectedChanged;

        // Not sure if this will ever be used since track removal should typically be
        // handled at a higher level, but this could work for lazy event handler removal
        public event TimelineTrackIndexEventHandler Removed;

        private readonly List<Clip> clips;
        private readonly ClipRangeCache cache;
        private readonly List<Clip> selectedClips;
        private double height = 60d;
        private string displayName = "Track";
        private SKColor colour;
        private bool isSelected;

        protected Track() {
            this.clips = new List<Clip>();
            this.Clips = new ReadOnlyCollection<Clip>(this.clips);
            this.cache = new ClipRangeCache();
            this.cache.FrameDataChanged += this.OnRangeCachedFrameDataChanged;
            this.colour = RenderUtils.RandomColour();
            this.selectedClips = new List<Clip>();
        }

        private void OnRangeCachedFrameDataChanged(ClipRangeCache handler) {
            this.Timeline?.UpdateLargestFrame();
        }

        public Track Clone() => this.Clone(TrackCloneOptions.Default);

        public Track Clone(TrackCloneOptions options) {
            string id = this.FactoryId;
            Track track = TrackFactory.Instance.NewTrack(id);
            this.LoadDataIntoClone(track, options);
            return track;
        }

        protected virtual void LoadDataIntoClone(Track clone, TrackCloneOptions options) {
            clone.height = this.height;
            clone.displayName = this.displayName;
            clone.colour = this.colour;
            if (options.CloneClips) {
                for (int i = 0; i < this.clips.Count; i++) {
                    clone.InsertClip(i, this.clips[i].Clone(options.ClipCloneOptions));
                }
            }
        }

        public void AddClip(Clip clip) => this.InsertClip(this.clips.Count, clip);

        public void InsertClip(int index, Clip clip) {
            if (this.clips.Contains(clip))
                throw new InvalidOperationException("This track already contains the clip");
            this.InternalInsertClipAt(index, clip);
            Clip.OnAddedToTrack(clip, this);
            this.ClipAdded?.Invoke(this, clip, index);
            this.InvalidateRender();
        }

        public bool RemoveClip(Clip clip) {
            int index = this.clips.IndexOf(clip);
            if (index == -1)
                return false;
            this.RemoveClipAt(index);
            return true;
        }

        public void RemoveClipAt(int index) {
            Clip clip = this.clips[index];
            this.InternalRemoveClipAt(index, clip);
            Clip.OnRemovedFromTrack(clip, this);
            this.ClipRemoved?.Invoke(this, clip, index);
            this.InvalidateRender();
        }

        public void MoveClipToTrack(int srcIndex, Track dstTrack, int dstIndex) {
            if (dstTrack == null)
                throw new ArgumentOutOfRangeException(nameof(dstTrack));
            if (dstIndex < 0 || dstIndex > dstTrack.clips.Count)
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is out of range");
            Clip clip = this.clips[srcIndex];
            this.InternalRemoveClipAt(srcIndex, clip);
            dstTrack.InternalInsertClipAt(dstIndex, clip);
            Clip.OnMovedToTrack(clip, this, dstTrack);
            this.ClipMovedTracks?.Invoke(clip, this, srcIndex, dstTrack, dstIndex);
            dstTrack.ClipMovedTracks?.Invoke(clip, this, srcIndex, dstTrack, dstIndex);
            this.InvalidateRender();
            dstTrack.InvalidateRender();
        }

        private void InternalInsertClipAt(int index, Clip clip) {
            this.clips.Insert(index, clip);
            this.cache.OnClipAdded(clip);
            if (clip.IsSelected)
                this.selectedClips.Add(clip);
        }

        private void InternalRemoveClipAt(int index, Clip clip) {
            this.clips.RemoveAt(index);
            this.cache.OnClipRemoved(clip);
            if (clip.IsSelected)
                this.selectedClips.Remove(clip);
            Timelines.Timeline.OnClipRemovedFromTrack(this, clip);
        }

        public virtual bool IsClipTypeAccepted(Clip clip) {
            return this.IsClipTypeAccepted(clip.GetType());
        }

        public abstract bool IsClipTypeAccepted(Type type);

        public bool IsRegionEmpty(FrameSpan span) => this.cache.IsRegionEmpty(span);

        public Clip GetClipAtFrame(long frame) => this.cache.GetPrimaryClipAt(frame);

        public virtual void Destroy() {
            for (int i = this.clips.Count - 1; i >= 0; i--) {
                Clip clip = this.clips[i];
                clip.Destroy();
                this.RemoveClipAt(i);
            }
        }

        public override string ToString() {
            return $"{this.GetType().Name} ({this.clips.Count.ToString()} clips between {this.cache.SmallestActiveFrame.ToString()} and {this.cache.LargestActiveFrame.ToString()})";
        }

        /// <summary>
        /// Adds all clips within the given frame span to the given list
        /// </summary>
        /// <param name="list">The destination list</param>
        /// <param name="span">The span range</param>
        public void CollectClipsInSpan(List<Clip> list, FrameSpan span) {
            this.cache.GetClipsInRange(list, span);
        }

        public List<Clip> GetClipsInSpan(FrameSpan span) {
            List<Clip> list = new List<Clip>();
            this.CollectClipsInSpan(list, span);
            return list;
        }

        internal static void OnAddedToTimeline(Track track, Timeline timeline) {
            track.Timeline = timeline;
        }

        #region Internal Access Helpers -- Used internally only

        internal static void OnRemovedFromTimeline1(Track track, Timeline timeline) {
            track.Timeline = null;
        }

        internal static void OnClipSpanChanged(Clip clip, FrameSpan oldSpan) {
            clip.Track?.cache.OnSpanChanged(clip, oldSpan);
        }

        // A 2nd version is required because the timeline's TrackRemoved event must be called after
        // the timeline reference is set to null, but before the track' own removed event is fired
        internal static void OnRemovedFromTimeline2(Timeline timeline, Track track, int index) {
            track.Removed?.Invoke(timeline, track, index);
        }

        internal static void OnIsClipSelectedChanged(Clip clip) {
            // If the track is null, it means the clip was either removed previously
            // and therefore its selection has already been processed, or has never been
            // added and therefore we don't care about the new selected state
            if (clip.Track == null)
                return;

            List<Clip> list = clip.Track.selectedClips;
            if (clip.IsSelected) {
                list.Add(clip);
            }
            else if (list.Count > 0) {
                if (list[0] == clip) {
                    list.RemoveAt(0);
                }
                else { // assume back to front removal
                    int index = list.LastIndexOf(clip);
                    if (index == -1) {
                        throw new Exception("Track was never selected");
                    }

                    list.RemoveAt(index);
                }
            }

            Timeline.OnIsClipSelectedChanged(clip);
        }

        #endregion

        public void ClearClipSelection() {
            // Use back to front removal since OnIsClipSelectedChanged can process
            // that more efficiently, and in general, back to front is more efficient
            List<Clip> list = this.selectedClips;
            for (int i = list.Count - 1; i >= 0; i--) {
                list[i].IsSelected = false;
            }
        }

        public void InvalidateRender() {
            this.Timeline?.Project?.RenderManager.InvalidateRender();
        }
    }
}