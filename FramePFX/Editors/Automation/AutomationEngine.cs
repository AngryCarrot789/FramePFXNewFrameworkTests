using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Automation {
    public static class AutomationEngine {
        public static void UpdateValues(Timeline timeline) {
            long playHead = timeline.PlayHeadPosition;
            foreach (Track track in timeline.Tracks) {
                track.AutomationData.Update(playHead);
                foreach (Clip clip in track.GetClipsAtFrame(playHead)) {
                    clip.AutomationData.Update(playHead);
                }
            }
        }
    }
}