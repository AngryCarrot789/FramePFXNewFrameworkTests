using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        private readonly BasicAutoBinder<VideoTrack> opacityBinder = new BasicAutoBinder<VideoTrack>(RangeBase.ValueProperty, nameof(VideoTrack.OpacityChanged), binder => binder.Model.Opacity, (binder, value) => binder.Model.Opacity = (double) value);

        public TimelineTrackListBoxItemContent_Video(TimelineTrackListBoxItem listItem) : base(listItem) {

        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_OpacitySlider") is NumberDragger dragger))
                throw new Exception("Missing PART_OpacitySlider");
            this.OpacityDragger = dragger;
            this.OpacityDragger.ValueChanged += (sender, args) => this.opacityBinder.OnControlValueChanged();
            if (this.opacityBinder.IsAttached)
                this.opacityBinder.Detatch();
            if (this.ListItem.Track is VideoTrack)
                this.opacityBinder.Attach(dragger, (VideoTrack) this.ListItem.Track);
        }

        public override void OnAddedToTimeline() {
            base.OnAddedToTimeline();
            if (this.ListItem.Track is VideoTrack && this.OpacityDragger != null) {
                if (this.opacityBinder.IsAttached)
                    this.opacityBinder.Detatch();
                this.opacityBinder.Attach(this.OpacityDragger, (VideoTrack) this.ListItem.Track);
            }
        }
    }

    /*

This is a version of the class without using the binder
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
     */
}