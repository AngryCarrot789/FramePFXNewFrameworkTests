using System;
using FramePFX.Destroying;

namespace FramePFX.Editors.Timelines.Tracks.Clips {
    public delegate void ClipSpanChangedEventHandler(Clip clip, FrameSpan oldSpan, FrameSpan newSpan);

    public class Clip : IDestroy {
        private FrameSpan span;

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

        public event ClipSpanChangedEventHandler SpanChanged;

        public Clip() {
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

        public virtual void Destroy() {

        }
    }
}