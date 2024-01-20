using System;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Controls.xclemence.RulerWPF;
using FramePFX.Editors.Timelines;

namespace FramePFX.Editors.Controls {
    public class FullTimelineControl : Control {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(FullTimelineControl), new PropertyMetadata(null, (d, e) => ((FullTimelineControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public TimelineTrackListBox TrackList { get; private set; }

        public ScrollViewer TrackListScrollViewer { get; private set; }

        public TimelineSequenceControl TimelineControl { get; private set; }

        public PlayheadPositionTextControl PlayHeadPositionPreview { get; private set; }

        public PlayHeadControl PlayHead { get; private set; }

        public Ruler Ruler { get; private set; }

        public FullTimelineControl() {
        }

        static FullTimelineControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FullTimelineControl), new FrameworkPropertyMetadata(typeof(FullTimelineControl)));
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_TrackListBox") is TimelineTrackListBox listBox))
                throw new Exception("Missing PART_TrackListBox");
            if (!(this.GetTemplateChild("PART_Timeline") is TimelineSequenceControl timeline))
                throw new Exception("Missing PART_TimelineControl");
            if (!(this.GetTemplateChild("PART_TrackListScrollViewer") is ScrollViewer scrollViewer))
                throw new Exception("Missing PART_TrackListScrollViewer");
            if (!(this.GetTemplateChild("PART_PlayheadPositionPreviewControl") is PlayheadPositionTextControl playheadPosPreview))
                throw new Exception("Missing PART_PlayheadPositionPreviewControl");
            if (!(this.GetTemplateChild("PART_Ruler") is Ruler ruler))
                throw new Exception("Missing PART_Ruler");
            if (!(this.GetTemplateChild("PART_PlayHeadControl") is PlayHeadControl playHead))
                throw new Exception("Missing PART_PlayHeadControl");
            this.TrackList = listBox;
            this.TimelineControl = timeline;
            this.TrackListScrollViewer = scrollViewer;
            this.PlayHeadPositionPreview = playheadPosPreview;
            this.Ruler = ruler;
            this.PlayHead = playHead;
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline != null) {
                oldTimeline.TotalFramesChanged -= this.OnTimelineTotalFramesChanged;
            }

            this.TimelineControl.Timeline = newTimeline;
            this.TrackList.Timeline = newTimeline;
            this.PlayHeadPositionPreview.Timeline = newTimeline;
            this.PlayHead.Timeline = newTimeline;
            if (newTimeline != null) {
                newTimeline.TotalFramesChanged += this.OnTimelineTotalFramesChanged;
                this.Ruler.MaxValue = newTimeline.TotalFrames;
            }
        }

        private void OnTimelineTotalFramesChanged(Timeline timeline) {
            this.Ruler.MaxValue = timeline.TotalFrames;
        }
    }
}