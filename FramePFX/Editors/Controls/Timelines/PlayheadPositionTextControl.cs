using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Timelines;

namespace FramePFX.Editors.Controls.Timelines {
    public class PlayheadPositionTextControl : Control {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(PlayheadPositionTextControl), new PropertyMetadata(null, (d, e) => ((PlayheadPositionTextControl) d).OnTimelineChanged((Timeline)e.OldValue, (Timeline)e.NewValue)));
        public static readonly DependencyProperty PlayHeadPositionProperty = DependencyProperty.Register("PlayHeadPosition", typeof(long), typeof(PlayheadPositionTextControl), new FrameworkPropertyMetadata(0L));
        public static readonly DependencyProperty TotalFrameDurationProperty = DependencyProperty.Register("TotalFrameDuration", typeof(long), typeof(PlayheadPositionTextControl), new FrameworkPropertyMetadata(0L));
        public static readonly DependencyProperty LargestFrameInUseProperty = DependencyProperty.Register("LargestFrameInUse", typeof(long), typeof(PlayheadPositionTextControl), new PropertyMetadata(0L));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public long PlayHeadPosition {
            get => (long) this.GetValue(PlayHeadPositionProperty);
            set => this.SetValue(PlayHeadPositionProperty, value);
        }

        public long TotalFrameDuration {
            get => (long) this.GetValue(TotalFrameDurationProperty);
            set => this.SetValue(TotalFrameDurationProperty, value);
        }

        public long LargestFrameInUse {
            get => (long) this.GetValue(LargestFrameInUseProperty);
            set => this.SetValue(LargestFrameInUseProperty, value);
        }

        private readonly Binder<Timeline> playHeadBinder;
        private readonly Binder<Timeline> totalFramesBinder;
        private readonly Binder<Timeline> largestFrameInUseBinder;

        public PlayheadPositionTextControl() {
            this.playHeadBinder = Binder<Timeline>.AutoSet(this, PlayHeadPositionProperty, nameof(this.Timeline.PlayHeadChanged), (b) => b.PlayHeadPosition, (b, v) => b.PlayHeadPosition = v);
            this.totalFramesBinder = Binder<Timeline>.AutoSet(this, TotalFrameDurationProperty, nameof(this.Timeline.TotalFramesChanged), (b) => b.TotalFrames, (b, v) => b.TotalFrames = v);
            this.largestFrameInUseBinder = Binder<Timeline>.Updaters(this, LargestFrameInUseProperty, nameof(this.Timeline.LargestFrameInUseChanged), obj => obj.Element.SetValue(LargestFrameInUseProperty, obj.Model.LargestFrameInUse), null);
        }

        static PlayheadPositionTextControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PlayheadPositionTextControl), new FrameworkPropertyMetadata(typeof(PlayheadPositionTextControl)));
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline != null) {
                this.totalFramesBinder.Detatch();
                this.playHeadBinder.Detatch();
                this.largestFrameInUseBinder.Detatch();
            }

            if (newTimeline != null) {
                this.totalFramesBinder.Attach(newTimeline);
                this.playHeadBinder.Attach(newTimeline);
                this.largestFrameInUseBinder.Attach(newTimeline);
            }
        }
    }
}