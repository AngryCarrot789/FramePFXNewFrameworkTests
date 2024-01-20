using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.ViewModels {
    /// <summary>
    /// A view model for a track controller item in the track list on the left side
    /// </summary>
    public class TrackListTrackViewModel : INotifyPropertyChanged, IAttachable {
        public event PropertyChangedEventHandler PropertyChanged;

        public Track Track { get; }

        public string DisplayName {
            get => this.Track.DisplayName;
            set => this.Track.DisplayName = value;
        }

        public TrackListTrackViewModel(Track track) {
            this.Track = track;
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaisePropertyChanged<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            field = newValue;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Attach() {
        }

        public void Detatch() {

        }
    }
}