using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines.ViewModels {
    public class TrackListVideoTrackViewModel : TrackListTrackViewModel {
        public new VideoTrack Track => (VideoTrack) base.Track;

        public double Opacity {
            get => this.Track.Opacity;
            set {
                this.Track.Opacity = value;
                this.RaisePropertyChanged();
            }
        }

        public TrackListVideoTrackViewModel(VideoTrack track) : base(track) {

        }
    }
}