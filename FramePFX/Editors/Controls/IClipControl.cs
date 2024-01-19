using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls {
    public interface IClipControl {
        ITrackControl Track { get; }

        Clip Model { get; }

        void OnAddingToTrack();
        void OnAddToTrack();
        void OnRemovingFromTrack();
        void OnRemovedFromTrack();
    }
}