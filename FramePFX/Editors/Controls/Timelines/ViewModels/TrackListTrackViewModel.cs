using System.ComponentModel;
using System.Runtime.CompilerServices;
using FramePFX.Annotations;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines.ViewModels {
    public class TrackListTrackViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName {
            get => this.Track.DisplayName;
            set {
                this.Track.DisplayName = value;
                this.RaisePropertyChanged();
            }
        }

        public Track Track { get; }

        public TrackListTrackViewModel(Track track) {
            this.Track = track;
        }

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            field = newValue;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}