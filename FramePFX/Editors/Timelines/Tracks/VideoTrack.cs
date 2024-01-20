using System;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Utils;

namespace FramePFX.Editors.Timelines.Tracks {
    public delegate void VideoTrackEventHandler(VideoTrack track);

    public class VideoTrack : Track {
        private double opacity;

        public double Opacity {
            get => this.opacity;
            set {
                if (Maths.Equals(this.opacity, value))
                    return;
                this.opacity = value;
                this.OpacityChanged?.Invoke(this);
            }
        }

        public event VideoTrackEventHandler OpacityChanged;

        public VideoTrack() {
            this.opacity = 1.0;
        }

        public override bool IsClipTypeAccepted(Type type) {
            return typeof(VideoClip).IsAssignableFrom(type);
        }
    }
}