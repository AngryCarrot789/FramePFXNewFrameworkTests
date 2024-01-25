using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FramePFX.Editors.Controls.Rulers;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Utils;

namespace FramePFX.Editors.Controls.Timelines {
    /// <summary>
    /// A control that represents the entire state of a timeline, with a timeline sequence editor, track list state editor, etc.
    /// </summary>
    public class TimelineControl : Control {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineControl), new PropertyMetadata(null, (d, e) => ((TimelineControl) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        public TimelineTrackListBox TrackList { get; private set; }

        public ScrollViewer TrackListScrollViewer { get; private set; }

        public ScrollViewer TimelineScrollViewer { get; private set; }

        public TimelineSequenceControl TimelineSequence { get; private set; }

        public TimelineScrollableContentGrid TimelineContentGrid { get; private set; }

        // The border that the TimelineControl is placed in
        public Border TimelineBorder { get; private set; }

        public PlayheadPositionTextControl PlayHeadPositionPreview { get; private set; }

        public PlayHeadControl PlayHead { get; private set; }

        public StopHeadControl StopHead { get; private set; }

        public Ruler Ruler { get; private set; }

        public Border RulerContainerBorder { get; private set; } // contains the ruler

        private readonly List<Button> timelineActionButtons;

        public TimelineControl() {
            this.MouseLeftButtonDown += (s, e) => {
                // ((TimelineControl) s).MovePlayHeadToMouseCursor(e.GetPosition((IInputElement) s).X + (this.TimelineScrollViewer?.HorizontalOffset ?? 0d), false);
            };

            this.timelineActionButtons = new List<Button>();
        }

        public Point GetTimelinePointFromClip(Point pointInClip) {
            return new Point(pointInClip.X + (this.TimelineScrollViewer?.HorizontalOffset ?? 0d), pointInClip.Y);
        }

        private void MovePlayHeadToMouseCursor(double x, bool enableThumbDragging = true, bool updateStopHead = true) {
            if (!(this.Timeline is Timeline timeline)) {
                return;
            }

            if (x >= 0d) {
                long frameX = TimelineUtils.PixelToFrame(x, timeline.Zoom, true);
                if (frameX == timeline.PlayHeadPosition) {
                    return;
                }

                if (frameX >= 0 && frameX < timeline.MaxDuration) {
                    timeline.PlayHeadPosition = frameX;
                    if (updateStopHead) {
                        timeline.StopHeadPosition = frameX;
                    }
                }

                if (enableThumbDragging) {
                    this.PlayHead.EnableDragging(new Point(x, 0));
                }
            }
        }

        public void SetPlayHeadToMouseCursor(double sequencePixelX, bool setStopHeadPosition = true) {
            // no need to add scrollviewer offset, since the sequencePixelX
            // will naturally include the horizontal offset kinda
            this.MovePlayHeadToMouseCursor(sequencePixelX, false, setStopHeadPosition);
        }

        static TimelineControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineControl), new FrameworkPropertyMetadata(typeof(TimelineControl)));
        }

        private void GetTemplateChild<T>(string name, out T value) where T : DependencyObject {
            if ((value = this.GetTemplateChild(name) as T) == null)
                throw new Exception("Missing part: " + name);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.GetTemplateChild("PART_TrackListBox", out TimelineTrackListBox trackListBox);
            this.GetTemplateChild("PART_Timeline", out TimelineSequenceControl timeline);
            this.GetTemplateChild("PART_TrackListScrollViewer", out ScrollViewer scrollViewer);
            this.GetTemplateChild("PART_SequenceScrollViewer", out ScrollViewer timelineScrollViewer);
            this.GetTemplateChild("PART_PlayheadPositionPreviewControl", out PlayheadPositionTextControl playheadPosPreview);
            this.GetTemplateChild("PART_Ruler", out Ruler ruler);
            this.GetTemplateChild("PART_PlayHeadControl", out PlayHeadControl playHead);
            this.GetTemplateChild("PART_StopHeadControl", out StopHeadControl stopHead);
            this.GetTemplateChild("PART_TimestampBoard", out Border timeStampBoard);
            this.GetTemplateChild("PART_TimelineSequenceBorder", out Border timelineBorder);
            this.GetTemplateChild("PART_ContentGrid", out TimelineScrollableContentGrid scrollableContent);

            // action buttons. need a better system because this is really not that good
            this.GetTemplateChild("PART_AddVideoTrackButton", out Button addVideoTrackButton);

            this.TrackList = trackListBox;
            this.TrackList.TimelineControl = this;

            this.TimelineSequence = timeline;
            timeline.TimelineControl = this;

            this.TimelineBorder = timelineBorder;
            this.TrackListScrollViewer = scrollViewer;
            this.PlayHeadPositionPreview = playheadPosPreview;
            this.Ruler = ruler;
            if (this.Timeline is Timeline myTimeline)
                this.Ruler.MaxValue = myTimeline.MaxDuration;

            this.PlayHead = playHead;
            this.StopHead = stopHead;
            this.TimelineScrollViewer = timelineScrollViewer;
            this.RulerContainerBorder = timeStampBoard;

            this.TimelineContentGrid = scrollableContent;
            scrollableContent.TimelineControl = this;

            timeStampBoard.MouseLeftButtonDown += (s, e) => this.MovePlayHeadToMouseCursor(e.GetPosition((IInputElement) s).X, true, false);

            this.CreateTimelineButtonAction(addVideoTrackButton, t => {
                VideoTrack track = new VideoTrack {DisplayName = "Video Track " + (t.Tracks.Count(x => x is VideoTrack) + 1).ToString()};
                t.AddTrack(track);
                track.InvalidateRender();
            });
        }

        private void CreateTimelineButtonAction(Button button, Action<Timeline> action) {
            this.timelineActionButtons.Add(button);
            button.Click += (sender, args) => {
                Timeline timeline = this.Timeline;
                if (timeline != null)
                    action(timeline);
            };
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline != null) {
                oldTimeline.MaxDurationChanged -= this.OnTimelineMaxDurationChanged;
                oldTimeline.ZoomTimeline -= this.OnTimelineZoomed;
                oldTimeline.TrackAdded -= this.OnTimelineTrackEvent;
                oldTimeline.TrackRemoved -= this.OnTimelineTrackEvent;
            }

            this.TimelineSequence.Timeline = newTimeline;
            this.TrackList.Timeline = newTimeline;
            this.PlayHeadPositionPreview.Timeline = newTimeline;
            this.PlayHead.Timeline = newTimeline;
            this.StopHead.Timeline = newTimeline;
            if (newTimeline != null) {
                newTimeline.MaxDurationChanged += this.OnTimelineMaxDurationChanged;
                newTimeline.ZoomTimeline += this.OnTimelineZoomed;
                newTimeline.TrackAdded += this.OnTimelineTrackEvent;
                newTimeline.TrackRemoved += this.OnTimelineTrackEvent;
                if (this.Ruler != null)
                    this.Ruler.MaxValue = newTimeline.MaxDuration;
                this.UpdateBorderThicknesses(newTimeline);
            }

            bool canExecute = newTimeline != null;
            foreach (Button button in this.timelineActionButtons) {
                button.IsEnabled = canExecute;
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
                    track.Track.Height = Maths.Clamp(track.Track.Height + (e.Delta / 120d * 8), TimelineClipControl.HeaderSize, 200d);
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

        private void OnTimelineMaxDurationChanged(Timeline timeline) {
            if (this.Ruler != null)
                this.Ruler.MaxValue = timeline.MaxDuration;
            if (this.TimelineContentGrid != null)
                this.TimelineContentGrid.Width = TimelineUtils.FrameToPixel(timeline.MaxDuration, timeline.Zoom);
        }

        private void OnTimelineZoomed(Timeline timeline, double oldzoom, double newzoom, ZoomType zoomtype) {
            this.TimelineSequence?.OnZoomChanged(newzoom);
            if (this.TimelineContentGrid != null)
                this.TimelineContentGrid.Width = TimelineUtils.FrameToPixel(timeline.MaxDuration, timeline.Zoom);

            ScrollViewer scroller = this.TimelineScrollViewer;
            if (scroller != null) {
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
        }

        public TimelineTrackControl GetTimelineControlFromTrack(Track track) {
            return this.TimelineSequence.GetTrackByModel(track);
        }
    }
}