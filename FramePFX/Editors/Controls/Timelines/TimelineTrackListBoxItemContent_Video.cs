using System;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        private readonly AutomationBinder<VideoTrack> opacityBinder = new AutomationBinder<VideoTrack>(VideoTrack.OpacityParameter, RangeBase.ValueProperty);

        public TimelineTrackListBoxItemContent_Video(TimelineTrackListBoxItem listItem) : base(listItem) {
            this.opacityBinder.UpdateModel += this.UpdateModelForOpacity;
        }

        private void UpdateModelForOpacity() {
            VideoTrack track = this.opacityBinder.Model;
            AutomationSequence sequence = track.AutomationData[this.opacityBinder.Parameter];
            // if (sequence.IsEmpty || sequence.IsOverrideEnabled) {
            //     object value = this.opacityBinder.Control.GetValue(this.opacityBinder.Property);
            //     sequence.DefaultKeyFrame.SetValueFromObject(value);
            // }
            // else {
            long frame = track.RelativePlayHead;
            int index = sequence.GetLastFrameExactlyAt(frame);
            KeyFrame keyFrame;
            if (index == -1) {
                index = sequence.AddNewKeyFrame(frame, out keyFrame);
            }
            else {
                keyFrame = sequence.GetKeyFrameAtIndex(index);
            }

            keyFrame.SetDoubleValue(this.OpacityDragger.Value);
            // }

            track.InvalidateRender();
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
                this.opacityBinder.Attach(this.OpacityDragger, track);
            }
        }

        public override void OnBeginRemovedFromTimeline() {
            base.OnBeginRemovedFromTimeline();
            this.opacityBinder.Detatch();
        }
    }
}