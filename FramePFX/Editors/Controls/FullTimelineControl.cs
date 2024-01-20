using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Controls.xclemence.RulerWPF;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;
using FramePFX.WPF.Utils;

namespace FramePFX.Editors.Controls {
    public class FullTimelineControl : Control {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(FullTimelineControl), new PropertyMetadata(null, (d, e) => ((FullTimelineControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public TimelineTrackListBox TrackList { get; private set; }

        public ScrollViewer TrackListScrollViewer { get; private set; }

        public ScrollViewer TimelineScrollViewer { get; private set; }

        public TimelineSequenceControl TimelineControl { get; private set; }

        // The border that the TimelineControl is placed in
        public Border TimelineBorder { get; private set; }

        public PlayheadPositionTextControl PlayHeadPositionPreview { get; private set; }

        public PlayHeadControl PlayHead { get; private set; }

        public Ruler Ruler { get; private set; }

        public Border RulerContainerBorder { get; private set; } // contains the ruler

        public FullTimelineControl() {
            this.MouseLeftButtonDown += (s, e) => this.MovePlayheadForMouseButtonEvent(e.GetPosition((IInputElement) s).X + this.TimelineScrollViewer?.HorizontalOffset ?? 0d, e, false);
        }

        private void MovePlayheadForMouseButtonEvent(double x, MouseButtonEventArgs e, bool enableThumbDragging = true) {
            if (!(this.Timeline is Timeline timeline)) {
                return;
            }

            if (x >= 0d) {
                long frameX = TimelineUtils.PixelToFrame(x, timeline.Zoom);
                if (frameX >= 0 && frameX < timeline.TotalFrames) {
                    timeline.PlayHeadPosition = frameX;
                }

                if (enableThumbDragging) {
                    this.PlayHead.EnableDragging(new Point(x, 0));
                }
            }
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
            if (!(this.GetTemplateChild("PART_SequenceScrollViewer") is ScrollViewer timelineScrollViewer))
                throw new Exception("Missing PART_SequenceScrollViewer");
            if (!(this.GetTemplateChild("PART_PlayheadPositionPreviewControl") is PlayheadPositionTextControl playheadPosPreview))
                throw new Exception("Missing PART_PlayheadPositionPreviewControl");
            if (!(this.GetTemplateChild("PART_Ruler") is Ruler ruler))
                throw new Exception("Missing PART_Ruler");
            if (!(this.GetTemplateChild("PART_PlayHeadControl") is PlayHeadControl playHead))
                throw new Exception("Missing PART_PlayHeadControl");
            if (!(this.GetTemplateChild("PART_TimestampBoard") is Border timeStampBoard))
                throw new Exception("Missing PART_TimestampBoard");
            if (!(this.GetTemplateChild("PART_TimelineSequenceBorder") is Border timelineBorder))
                throw new Exception("Missing PART_TimelineSequenceBorder");

            this.TrackList = listBox;
            this.TimelineControl = timeline;
            this.TimelineBorder = timelineBorder;
            this.TrackListScrollViewer = scrollViewer;
            this.PlayHeadPositionPreview = playheadPosPreview;
            this.Ruler = ruler;
            this.PlayHead = playHead;
            this.TimelineScrollViewer = timelineScrollViewer;
            this.RulerContainerBorder = timeStampBoard;

            timeStampBoard.MouseLeftButtonDown += (s, e) => this.MovePlayheadForMouseButtonEvent(e.GetPosition((IInputElement) s).X, e, true);
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline != null) {
                oldTimeline.TotalFramesChanged -= this.OnTimelineTotalFramesChanged;
                oldTimeline.ZoomTimeline -= this.OnTimelineZoomed;
                oldTimeline.TrackAdded -= this.OnTimelineTrackEvent;
                oldTimeline.TrackRemoved -= this.OnTimelineTrackEvent;
            }

            this.TimelineControl.Timeline = newTimeline;
            this.TrackList.Timeline = newTimeline;
            this.PlayHeadPositionPreview.Timeline = newTimeline;
            this.PlayHead.Timeline = newTimeline;
            if (newTimeline != null) {
                newTimeline.TotalFramesChanged += this.OnTimelineTotalFramesChanged;
                newTimeline.ZoomTimeline += this.OnTimelineZoomed;
                newTimeline.TrackAdded += this.OnTimelineTrackEvent;
                newTimeline.TrackRemoved += this.OnTimelineTrackEvent;
                this.Ruler.MaxValue = newTimeline.TotalFrames;
                this.UpdateBorderThicknesses(newTimeline);
            }
        }

        private void OnTimelineTrackEvent(Timeline timeline, Track track, int index) {
            this.UpdateBorderThicknesses(timeline);
        }

        private void UpdateBorderThicknesses(Timeline timeline) {
            // Just a cool feature to hide the border when there's no tracks, not necessary but meh
            Thickness thickness = new Thickness(0, 0, 0, (timeline.Tracks.Count < 1) ? 0 : 1);
            this.TimelineBorder.BorderThickness = thickness;
            this.TrackList.BorderThickness = thickness;
        }

        private void OnTimelineTotalFramesChanged(Timeline timeline) {
            this.Ruler.MaxValue = timeline.TotalFrames;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
            base.OnPreviewMouseWheel(e);
            if (e.Handled) {
                return;
            }

            ScrollViewer scroller = this.TimelineScrollViewer;
            if (scroller == null) {
                return;
            }

            Timeline timeline = this.Timeline;
            if (timeline == null) {
                return;
            }

            ModifierKeys mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Alt) != 0) {
                if (VisualTreeUtils.GetParent<TimelineTrackControl>(e.OriginalSource as DependencyObject) is TimelineTrackControl track) {
                    track.Track.Height = Maths.Clamp(track.Track.Height + (e.Delta / 120d) * 20d, TimelineClipControl.HeaderSize, 200d);
                }

                e.Handled = true;
            }
            else if ((mods & ModifierKeys.Control) != 0) {
                e.Handled = true;
                bool shift = (mods & ModifierKeys.Shift) != 0;
                double multiplier = (shift ? 0.2 : 0.4);
                if (e.Delta > 0) {
                    multiplier = 1d + multiplier;
                }
                else {
                    multiplier = 1d - multiplier;
                }

                double oldzoom = timeline.Zoom;
                double newzoom = Math.Max(oldzoom * multiplier, 0.0001d);
                double minzoom = scroller.ViewportWidth / (scroller.ExtentWidth / oldzoom); // add 0.000000000000001 to never disable scroll bar
                newzoom = Math.Max(minzoom, newzoom);
                timeline.SetZoom(newzoom, ZoomType.Direct); // let the coerce function clamp the zoom value
                newzoom = timeline.Zoom;

                // managed to get zooming towards the cursor working
                double mouse_x = e.GetPosition(scroller).X;
                double target_offset = (scroller.HorizontalOffset + mouse_x) / oldzoom;
                double scaled_target_offset = target_offset * newzoom;
                double new_offset = scaled_target_offset - mouse_x;
                scroller.ScrollToHorizontalOffset(new_offset);
            }
            else if ((mods & ModifierKeys.Shift) != 0) {
                if (e.Delta < 0) {
                    for (int i = 0; i < 6; i++) {
                        scroller.LineRight();
                    }
                }
                else {
                    for (int i = 0; i < 6; i++) {
                        scroller.LineLeft();
                    }
                }
                e.Handled = true;
            }
        }

        private void OnTimelineZoomed(Timeline timeline, double oldzoom, double newzoom, ZoomType zoomtype) {
            ScrollViewer scroller = this.TimelineScrollViewer;
            if (scroller == null) {
                return;
            }

            switch (zoomtype) {
                case ZoomType.Direct: break;
                case ZoomType.ViewPortBegin: {
                    break;
                }
                case ZoomType.ViewPortMiddle: {
                    break;
                }
                case ZoomType.ViewPortEnd: {
                    break;
                }
                case ZoomType.PlayHead: {
                    break;
                }
                case ZoomType.MouseCursor: {
                    double mouse_x = Mouse.GetPosition(scroller).X;
                    double target_offset = (scroller.HorizontalOffset + mouse_x) / oldzoom;
                    double scaled_target_offset = target_offset * newzoom;
                    double new_offset = scaled_target_offset - mouse_x;
                    scroller.ScrollToHorizontalOffset(new_offset);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(zoomtype), zoomtype, null);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

        }
    }
}