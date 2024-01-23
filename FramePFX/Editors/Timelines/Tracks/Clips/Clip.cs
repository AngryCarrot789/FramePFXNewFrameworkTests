using System;
using FramePFX.Destroying;
using FramePFX.Editors.Automation;
using FramePFX.Editors.Factories;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void ClipSpanChangedEventHandler(Clip clip, FrameSpan oldSpan, FrameSpan newSpan);
    public delegate void ClipEventHandler(Clip clip);

    public abstract class Clip : IAutomatable, IStrictFrameRange, IDestroy {
        private FrameSpan span;
        private string displayName;
        private bool isSelected;

        public Track Track { get; private set; }

        public Timeline Timeline => this.Track?.Timeline;

        public long RelativePlayHead => this.Timeline.PlayHeadPosition - this.span.Begin;

        public AutomationData AutomationData { get; }

        public FrameSpan FrameSpan {
            get => this.span;
            set {
                FrameSpan oldSpan = this.span;
                if (oldSpan == value)
                    return;
                this.span = value;
                Track.OnClipSpanChanged(this, oldSpan);
                this.OnFrameSpanChanged(oldSpan, value);
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
                Track.OnIsClipSelectedChanged(this);
                this.IsSelectedChanged?.Invoke(this);
            }
        }

        public string FactoryId => ClipFactory.Instance.GetId(this.GetType());

        public event ClipSpanChangedEventHandler FrameSpanChanged;
        public event ClipEventHandler DisplayNameChanged;
        public event ClipEventHandler IsSelectedChanged;

        protected Clip() {
            this.AutomationData = new AutomationData(this);
        }

        protected virtual void OnFrameSpanChanged(FrameSpan oldSpan, FrameSpan newSpan) {
            this.FrameSpanChanged?.Invoke(this, oldSpan, newSpan);
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

        public bool IntersectsFrameAt(long playHead) {
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
            FrameSpan spanRight = FrameSpan.FromIndex(spanLeft.EndIndex, this.FrameSpan.EndIndex);

            Clip clone = this.Clone();
            this.Track.AddClip(clone);

            this.FrameSpan = spanLeft;
            clone.FrameSpan = spanRight;
        }

        public long ConvertRelativeToTimelineFrame(long relative) => this.span.Begin + relative;

        public long ConvertTimelineToRelativeFrame(long timeline, out bool inRange) {
            long frame = timeline - this.span.Begin;
            inRange = frame >= 0 && frame < this.span.Duration;
            return frame;
        }

        public bool IsTimelineFrameInRange(long timeline) {
            long frame = timeline - this.span.Begin;
            return frame >= 0 && frame < this.span.Duration;
        }

        public virtual void Destroy() {

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
    }
}