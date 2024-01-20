using System;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Timelines.Tracks {
    public class VideoTrack : Track {
        public VideoTrack() {
        }

        public override bool IsClipTypeAccepted(Type type) {
            return typeof(VideoClip).IsAssignableFrom(type);
        }
    }
}