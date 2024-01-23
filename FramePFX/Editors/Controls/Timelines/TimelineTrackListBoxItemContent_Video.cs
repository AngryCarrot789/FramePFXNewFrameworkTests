using System;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        private readonly AutomationBinder<VideoTrack> opacityBinder = new AutomationBinder<VideoTrack>(VideoTrack.OpacityParameter);

        public TimelineTrackListBoxItemContent_Video(TimelineTrackListBoxItem listItem) : base(listItem) {
            this.opacityBinder.UpdateModel += UpdateModelForOpacity;
            this.opacityBinder.UpdateControl += UpdateControlForOpacity;
        }

        private static void UpdateModelForOpacity(AutomationBinder<VideoTrack> binder) {
            TimelineTrackListBoxItemContent_Video control = (TimelineTrackListBoxItemContent_Video) binder.Control;
            VideoTrack track = control.opacityBinder.Model;
            AutomationSequence sequence = track.AutomationData[control.opacityBinder.Parameter];
            if (sequence.IsEmpty || sequence.IsOverrideEnabled) {
                sequence.DefaultKeyFrame.SetDoubleValue(control.OpacityDragger.Value);
            }
            else {
                long frame = track.RelativePlayHead;
                int index = sequence.GetLastFrameExactlyAt(frame);
                KeyFrame keyFrame;
                if (index == -1) {
                    index = sequence.AddNewKeyFrame(frame, out keyFrame);
                }
                else {
                    keyFrame = sequence.GetKeyFrameAtIndex(index);
                }

                keyFrame.SetDoubleValue(control.OpacityDragger.Value);
            }

            track.InvalidateRender();
        }

        private static void UpdateControlForOpacity(AutomationBinder<VideoTrack> binder) {
            TimelineTrackListBoxItemContent_Video control = (TimelineTrackListBoxItemContent_Video) binder.Control;
            control.OpacityDragger.Value = binder.Model.Opacity;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_OpacitySlider") is NumberDragger dragger))
                throw new Exception("Missing PART_OpacitySlider");
            this.OpacityDragger = dragger;
            this.OpacityDragger.ValueChanged += (sender, args) => this.opacityBinder.OnControlValueChanged();
        }

        public override void OnAddedToTimeline() {
            base.OnAddedToTimeline();
            if (this.ListItem.Track is VideoTrack track) {
                this.opacityBinder.Attach(this, track);
            }
        }

        public override void OnBeginRemovedFromTimeline() {
            base.OnBeginRemovedFromTimeline();
            this.opacityBinder.Detatch();
        }
    }
}