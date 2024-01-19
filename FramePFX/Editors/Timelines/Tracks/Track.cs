using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FramePFX.Destroying;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Timelines.Tracks {
    public delegate void TrackEventHandler(Track track);
    public delegate void TrackClipIndexEventHandler(Track track, Clip clip, int index);
    public delegate void ClipMovedEventHandler(Clip clip, Track oldTrack, int oldIndex, Track newTrack, int newIndex);

    public abstract class Track : IDestroy {
        private readonly List<Clip> clips;
        private double height = 60d;
        private string displayName = "Track";

        public event TrackClipIndexEventHandler ClipAdded;
        public event TrackClipIndexEventHandler ClipRemoved;
        public event ClipMovedEventHandler ClipMovedTracks;

        public event TrackEventHandler HeightChanged;
        public event TrackEventHandler DisplayNameChanged;

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

        protected Track() {
            this.clips = new List<Clip>();
            this.Clips = new ReadOnlyCollection<Clip>(this.clips);
        }

        public void AddClip(Clip clip) => this.InsertClip(this.clips.Count, clip);

        public void InsertClip(int index, Clip clip) {
            if (this.clips.Contains(clip))
                throw new InvalidOperationException("This track already contains the clip");
            this.clips.Insert(index, clip);
            Clip.OnAddedToTrack(clip, this);
            this.ClipAdded?.Invoke(this, clip, index);
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
            this.clips.RemoveAt(index);
            Clip.OnRemovedFromTrack(clip, this);
            this.ClipRemoved?.Invoke(this, clip, index);
        }

        public void MoveClipToTrack(int srcIndex, Track dstTrack, int dstIndex) {
            if (dstTrack == null)
                throw new ArgumentOutOfRangeException(nameof(dstTrack));
            if (dstIndex < 0 || dstIndex > dstTrack.clips.Count)
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is out of range");
            Clip clip = this.clips[srcIndex];
            this.clips.RemoveAt(srcIndex);
            dstTrack.clips.Insert(dstIndex, clip);
            Clip.OnMovedToTrack(clip, this, dstTrack);
            this.ClipMovedTracks?.Invoke(clip, this, srcIndex, dstTrack, dstIndex);
            dstTrack.ClipMovedTracks?.Invoke(clip, this, srcIndex, dstTrack, dstIndex);
        }

        public virtual bool IsClipTypeAccepted(Clip clip) {
            return this.IsClipTypeAccepted(clip.GetType());
        }

        public abstract bool IsClipTypeAccepted(Type type);

        internal static void OnAddedToTimeline(Track track, Timeline timeline) {
            track.Timeline = timeline;
        }

        internal static void OnRemovedFromTimeline(Track track, Timeline timeline) {
            track.Timeline = null;
        }

        public virtual void Destroy() {
            for (int i = this.clips.Count - 1; i >= 0; i--) {
                Clip clip = this.clips[i];
                clip.Destroy();
                this.RemoveClipAt(i);
            }
        }
    }
}