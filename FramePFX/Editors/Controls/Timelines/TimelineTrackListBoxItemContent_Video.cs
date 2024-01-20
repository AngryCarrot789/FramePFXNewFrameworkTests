using System;
using System.Windows;
using FramePFX.Editors.Controls.NumDragger;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        private bool isUpdatingOpacity;

        public TimelineTrackListBoxItemContent_Video(TimelineTrackListBoxItem listItem) : base(listItem) {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_OpacitySlider") is NumberDragger dragger))
                throw new Exception("Missing PART_OpacitySlider");
            this.OpacityDragger = dragger;
            dragger.ValueChanged += this.OnDraggerOpacityChanged;
            this.UpdateOpacityControl();
        }

        private void OnDraggerOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.isUpdatingOpacity)
                return;
            this.UpdateOpacityModel();
        }

        private void UpdateOpacityControl() {
            if (this.OpacityDragger != null && this.ListItem.Track is VideoTrack track) {
                this.isUpdatingOpacity = true;
                this.OpacityDragger.Value = track.Opacity;
                this.isUpdatingOpacity = false;
            }
        }

        private void UpdateOpacityModel() {
            if (this.OpacityDragger != null && this.ListItem.Track is VideoTrack track) {
                track.Opacity = this.OpacityDragger.Value;
            }
        }

        private void OnOpacityChanged(VideoTrack track) {
            this.UpdateOpacityControl();
        }

        public override void OnAddedToTimeline() {
            base.OnAddedToTimeline();
            ((VideoTrack) this.ListItem.Track).OpacityChanged += this.OnOpacityChanged;
            this.UpdateOpacityControl();
        }

        public override void OnRemovedFromTimeline() {
            base.OnRemovedFromTimeline();
            ((VideoTrack) this.ListItem.Track).OpacityChanged -= this.OnOpacityChanged;
        }
    }
}