using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls {
    public class TimelineClipControl : FrameworkElement {
        private long frameBegin;
        private long frameDuration;

        public TimelineTrackControl Track { get; set; }

        public long FrameBegin {
            get => this.frameBegin;
            private set {
                this.frameBegin = value;
                this.InvalidateMeasure();
                this.InvalidateArrange();
                // this.InvalidateVisual();
                this.Track.InvalidateArrange();
            }
        }

        public long FrameDuration {
            get => this.frameDuration;
            private set {
                this.frameDuration = value;
                this.InvalidateMeasure();
                this.InvalidateArrange();
                // this.InvalidateVisual();
                this.Track.InvalidateArrange();
            }
        }

        public double TimelineZoom => this.Track?.Timeline?.UnitZoom ?? 1d;
        public double PixelBegin => this.frameBegin * this.TimelineZoom;
        public double PixelWidth => this.frameDuration * this.TimelineZoom;

        public Clip Model { get; }

        private DragState dragState;
        private Point clickPoint;
        private bool isUpdatingFrameSpanFromDrag;
        private long accumulator;
        private long accumulatorL;
        private long accumulatorR;

        public TimelineClipControl(Clip clip) {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.Model = clip;
            this.UseLayoutRounding = true;
        }

        #region Model Binding

        private void SetSizeFromSpan(FrameSpan span) {
            this.FrameBegin = span.Begin;
            this.FrameDuration = span.Duration;
        }

        private void OnClipSpanChanged(Clip clip, FrameSpan oldspan, FrameSpan newspan) {
            this.SetSizeFromSpan(newspan);
        }

        public void OnAdding() {
            this.Model.SpanChanged += this.OnClipSpanChanged;
            this.SetSizeFromSpan(this.Model.Span);
        }

        public void OnAdded() {

        }

        public void OnRemoving() {
            this.Model.SpanChanged -= this.OnClipSpanChanged;
        }

        public void OnRemoved() {

        }

        #endregion

        protected override void OnMouseDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);
            this.clickPoint = e.GetPosition(this);
            this.dragState = DragState.Initiated;
            if (!this.IsMouseCaptured)
                this.CaptureMouse();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            this.dragState = DragState.None;
            this.ReleaseMouseCapture();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (this.isUpdatingFrameSpanFromDrag) {
                return;
            }

            if (this.IsMovingBetweenTracks) {
                this.IsMovingBetweenTracks = false;
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed) {
                this.dragState = DragState.None;
                this.ReleaseMouseCapture();
                return;
            }

            Point mpos = e.GetPosition(this);
            if (this.dragState == DragState.Initiated) {
                if (Math.Abs(mpos.X - this.clickPoint.X) < 4d && Math.Abs(mpos.Y - this.clickPoint.Y) < 4d) {
                    return;
                }

                if (this.clickPoint.X <= 4d) {
                    this.dragState = DragState.DragLeftEdge;
                }
                else if (this.clickPoint.X >= (this.ActualWidth - 4d)) {
                    this.dragState = DragState.DragRightEdge;
                }
                else {
                    this.dragState = DragState.DragBody;
                }

                this.accumulator = 0;
            }

            if (this.dragState == DragState.DragBody) {
                TimelineControl timelineCtrl = this.Track?.Timeline;
                Vector diff = mpos - this.clickPoint;
                if (Math.Abs(diff.X) >= 1.0d) {
                    double zoom = timelineCtrl?.UnitZoom ?? 1d;
                    long offset = (long) Math.Round(diff.X / zoom);
                    if (offset != 0) {
                        FrameSpan currSpan = this.Model.Span;
                        if ((currSpan.Begin + offset) < 0) {
                            offset = -currSpan.Begin;
                        }

                        if (offset != 0) {
                            long newBegin = (currSpan.Begin + offset) + this.accumulator;
                            this.accumulator = 0;
                            if (newBegin < 0) {
                                this.accumulator = -newBegin;
                                newBegin = 0;
                            }


                            FrameSpan newSpan = new FrameSpan(newBegin, currSpan.Duration);
                            long newEndIndex = newSpan.EndIndex;
                            if (timelineCtrl != null && newEndIndex > timelineCtrl.Timeline.TotalFrames) {
                                timelineCtrl.Timeline.TotalFrames = newEndIndex + 300;
                            }

                            this.isUpdatingFrameSpanFromDrag = true;
                            this.Model.Span = newSpan;
                            this.isUpdatingFrameSpanFromDrag = false;
                        }
                    }
                }

                if (Math.Abs(diff.Y) >= 1.0d && timelineCtrl?.Timeline is Timeline timeline) {
                    int trackIndex = timeline.Tracks.IndexOf(this.Model.Track);
                    const double area = 0;
                    if (mpos.Y < Math.Min(area, this.clickPoint.Y)) {
                        if (trackIndex < 1) {
                            return;
                        }

                        this.IsMovingBetweenTracks = true;
                        this.Model.MoveToTrack(timeline.Tracks[trackIndex - 1]);
                    }
                    else if (mpos.Y > (this.ActualHeight - area)) {
                        if (trackIndex >= (timeline.Tracks.Count - 1)) {
                            return;
                        }

                        this.IsMovingBetweenTracks = true;
                        this.Model.MoveToTrack(timeline.Tracks[trackIndex + 1]);
                    }
                }
            }
            else if (this.dragState == DragState.DragLeftEdge || this.dragState == DragState.DragRightEdge) {
                TimelineControl timelineCtrl = this.Track?.Timeline;
                Vector diff = mpos - this.clickPoint;
                if (Math.Abs(diff.X) >= 1.0d) {
                    double zoom = timelineCtrl?.UnitZoom ?? 1d;
                    long offset = (long) Math.Round(diff.X / zoom);
                    if (offset != 0) {
                        FrameSpan currSpan = this.Model.Span;
                        if (this.dragState == DragState.DragRightEdge) {
                            long newEndIndex = currSpan.EndIndex + offset + this.accumulatorR;
                            this.accumulatorR = 0;
                            if (newEndIndex <= currSpan.Begin) {
                                this.accumulatorR += (currSpan.Begin - newEndIndex);
                                newEndIndex = currSpan.Begin + 1;
                            }

                            FrameSpan newSpan = FrameSpan.FromIndex(currSpan.Begin, newEndIndex);
                            if (timelineCtrl != null && newEndIndex > timelineCtrl.Timeline.TotalFrames) {
                                timelineCtrl.Timeline.TotalFrames = newEndIndex + 300;
                            }

                            this.isUpdatingFrameSpanFromDrag = true;
                            this.Model.Span = newSpan;
                            this.isUpdatingFrameSpanFromDrag = false;
                            this.clickPoint = mpos;
                            // Debug.WriteLine($"Old = {currSpan}, New = {newSpan}, Offset = {diff}");
                        }
                        else {
                            if ((currSpan.Begin + offset) < 0) {
                                offset = -currSpan.Begin;
                            }

                            if (offset != 0) {
                                long currEndIndex = currSpan.EndIndex;
                                long newBegin = (currSpan.Begin + offset) + this.accumulator;
                                this.accumulator = 0;
                                if (newBegin < 0) {
                                    this.accumulator = -newBegin;
                                    newBegin = 0;
                                }

                                newBegin += this.accumulatorL;
                                this.accumulatorL = 0;
                                if (newBegin >= currEndIndex) {
                                    this.accumulatorL -= (newBegin - currEndIndex);
                                    newBegin = currEndIndex - 1;
                                }

                                FrameSpan newSpan = FrameSpan.FromIndex(newBegin, currEndIndex);
                                long newEndIndex = newSpan.EndIndex;
                                if (timelineCtrl != null && newEndIndex > timelineCtrl.Timeline.TotalFrames) {
                                    timelineCtrl.Timeline.TotalFrames = newEndIndex + 300;
                                }

                                this.isUpdatingFrameSpanFromDrag = true;
                                this.Model.Span = newSpan;
                                this.isUpdatingFrameSpanFromDrag = false;
                            }
                        }
                    }
                }
            }
        }

        public bool IsMovingBetweenTracks { get; set; }

        protected override Size MeasureOverride(Size availableSize) {
            return new Size(this.PixelWidth, 0d);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            return new Size(this.PixelWidth, finalSize.Height);
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);
            Rect rect = new Rect(new Point(), this.RenderSize);
            dc.DrawRectangle(SystemColors.ControlDarkBrush, null, rect);
            if (this.Track?.ClipHeaderColour is Brush headerBrush) {
                dc.DrawRectangle(headerBrush, null, new Rect(0, 0, rect.Width, Math.Min(rect.Height, 20d)));
            }

            // Pen pen = new Pen(Brushes.Black, 1d);
            // dc.DrawLine(pen, new Point(0d, 0d), new Point(0d, rect.Height));
            // dc.DrawLine(pen, new Point(rect.Width - 1d, 0d), new Point(rect.Width - 1d, rect.Height));
        }

        public void OnZoomChanged(double newZoom) {
            this.InvalidateMeasure();
        }

        private enum DragState {
            None,
            Initiated,
            DragBody,
            DragLeftEdge,
            DragRightEdge
        }
    }
}
