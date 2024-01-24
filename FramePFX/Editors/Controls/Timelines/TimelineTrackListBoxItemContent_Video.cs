using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Controls.Automation;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        public ToggleButton VisibilityButton { get; private set; }

        public VideoTrack MyTrack { get; private set; }

        private readonly AutomationBinder<VideoTrack> opacityBinder = new AutomationBinder<VideoTrack>(VideoTrack.OpacityParameter);
        private readonly AutomationBinder<VideoTrack> visibilityBinder = new AutomationBinder<VideoTrack>(VideoTrack.VisibleParameter);

        public TimelineTrackListBoxItemContent_Video() {
            this.opacityBinder.UpdateModel += UpdateOpacityForModel;
            this.opacityBinder.UpdateControl += UpdateOpacityForControl;
            this.visibilityBinder.UpdateModel += UpdateVisibilityForModel;
            this.visibilityBinder.UpdateControl += UpdateVisibilityForControl;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_OpacitySlider") is NumberDragger dragger))
                throw new Exception("Missing PART_OpacitySlider");
            if (!(this.GetTemplateChild("PART_VisibilityButton") is ToggleButton visibilityButton))
                throw new Exception("Missing PART_VisibilityButton");
            this.OpacityDragger = dragger;
            this.OpacityDragger.ValueChanged += (sender, args) => this.opacityBinder.OnControlValueChanged();

            this.VisibilityButton = visibilityButton;
            this.VisibilityButton.Checked += this.VisibilityCheckedChanged;
            this.VisibilityButton.Unchecked += this.VisibilityCheckedChanged;
        }

        private void VisibilityCheckedChanged(object sender, RoutedEventArgs e) {
            this.visibilityBinder.OnControlValueChanged();
        }

        private static void UpdateOpacityForModel(AutomationBinder<VideoTrack> binder) {
            AutomatedControlUtils.SetDefaultKeyFrameOrAddNew(binder.Model, ((TimelineTrackListBoxItemContent_Video) binder.Control).OpacityDragger, binder.Parameter, RangeBase.ValueProperty);
            binder.Model.InvalidateRender();
        }

        private static void UpdateOpacityForControl(AutomationBinder<VideoTrack> binder) {
            TimelineTrackListBoxItemContent_Video control = (TimelineTrackListBoxItemContent_Video) binder.Control;
            control.OpacityDragger.Value = binder.Model.Opacity;
        }

        private static void UpdateVisibilityForModel(AutomationBinder<VideoTrack> binder) {
            AutomatedControlUtils.SetDefaultKeyFrameOrAddNew(binder.Model, ((TimelineTrackListBoxItemContent_Video) binder.Control).VisibilityButton, binder.Parameter, ToggleButton.IsCheckedProperty);
            binder.Model.InvalidateRender();
        }

        private static void UpdateVisibilityForControl(AutomationBinder<VideoTrack> binder) {
            TimelineTrackListBoxItemContent_Video control = (TimelineTrackListBoxItemContent_Video) binder.Control;
            control.VisibilityButton.IsChecked = binder.Model.Visible;
        }

        protected override void OnConnected() {
            base.OnConnected();
            this.MyTrack = (VideoTrack) this.Owner.Track;
            this.opacityBinder.Attach(this, this.MyTrack);
            this.visibilityBinder.Attach(this, this.MyTrack);
        }

        protected override void OnDisconnected() {
            base.OnDisconnected();
            this.opacityBinder.Detatch();
            this.visibilityBinder.Detatch();
        }
    }
}