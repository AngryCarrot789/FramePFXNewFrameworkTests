using System;
using System.Windows;
using FramePFX.Destroying;
using FramePFX.Editors.Factories;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void ClipSpanChangedEventHandler(Clip clip, FrameSpan oldSpan, FrameSpan newSpan);
    public delegate void ClipEventHandler(Clip clip);

    public abstract class Clip : IDestroy {
        private FrameSpan span;
        private string displayName;
        private bool isSelected;

        public Track Track { get; private set; }

        public FrameSpan Span {
            get => this.span;
            set {
                FrameSpan oldValue = this.span;
                if (oldValue == value)
                    return;
                this.span = value;
                this.SpanChanged?.Invoke(this, oldValue, value);
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

        public bool IsSelected {
            get => this.isSelected;
            set {
                if (this.isSelected == value)
                    return;
                this.isSelected = value;
                this.SelectionChanged?.Invoke(this);
            }
        }

        public string FactoryId => ClipFactory.Instance.GetId(this.GetType());

        public event ClipSpanChangedEventHandler SpanChanged;
        public event ClipEventHandler DisplayNameChanged;
        public event ClipEventHandler SelectionChanged;

        public Clip() {
        }

        public Clip Clone() => this.Clone(ClipCloneOptions.Default);

        public Clip Clone(ClipCloneOptions options) {
            string id = this.FactoryId;
            Clip clone = ClipFactory.Instance.NewClip(id);
            this.LoadDataIntoClone(clone, options);
            return clone;
        }

        protected virtual void LoadDataIntoClone(Clip clone, ClipCloneOptions options) {
            clone.span = this.span;
            clone.displayName = this.displayName;
        }

        internal static void OnAddedToTrack(Clip clip, Track track) {
            clip.Track = track;
        }

        internal static void OnRemovedFromTrack(Clip clip, Track track) {
            clip.Track = null;
        }

        internal static void OnMovedToTrack(Clip clip, Track oldTrack, Track newTrack) {
            clip.Track = newTrack;
        }

        public void MoveToTrack(Track dstTrack) {
            this.MoveToTrack(dstTrack, dstTrack.Clips.Count);
        }

        public void MoveToTrack(Track dstTrack, int dstIndex) {
            if (this.Track == null) {
                dstTrack.InsertClip(dstIndex, this);
                return;
            }

            int index = this.Track.Clips.IndexOf(this);
            if (index == -1) {
                throw new Exception("Clip did not exist in its owner track");
            }

            this.Track.MoveClipToTrack(index, dstTrack, dstIndex);
        }

        public bool IsPlayHeadIntersecting(long playHead) {
            return this.span.Intersects(playHead);
        }

        /// <summary>
        /// Shrinks this clips and creates a clone in front of this clip, effectively "splitting" this clip into 2
        /// </summary>
        /// <param name="offset">The frame to split this clip at, relative to this clip</param>
        public void CutAt(long offset) {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
            if (offset == 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be zero");
            if (offset >= this.span.Duration)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot exceed our span's range");
            long begin = this.span.Begin;
            FrameSpan spanLeft = FrameSpan.FromIndex(begin, begin + offset);
            FrameSpan spanRight = FrameSpan.FromIndex(spanLeft.EndIndex, this.Span.EndIndex);

            Clip clone = this.Clone();
            this.Track.AddClip(clone);

            this.Span = spanLeft;
            clone.Span = spanRight;
        }

        public virtual void Destroy() {

        }
    }
}