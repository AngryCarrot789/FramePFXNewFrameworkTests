using System;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Controls.Dragger;
using FramePFX.Editors.Timelines.Tracks;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent_Video : TimelineTrackListBoxItemContent {
        public NumberDragger OpacityDragger { get; private set; }

        private readonly AutomationBinder<VideoTrack> opacityBinder = new AutomationBinder<VideoTrack>(VideoTrack.OpacityParameter, RangeBase.ValueProperty);

        public TimelineTrackListBoxItemContent_Video(TimelineTrackListBoxItem listItem) : base(listItem) {

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