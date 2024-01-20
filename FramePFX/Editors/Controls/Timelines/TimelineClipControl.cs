using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks.Clips;
using Timeline = FramePFX.Editors.Timelines.Timeline;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineClipControl : Control {
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(TimelineClipControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string DisplayName {
            get => (string) this.GetValue(DisplayNameProperty);
            set => this.SetValue(DisplayNameProperty, value);
        }

        public TimelineTrackControl Track { get; set; }

        public long FrameBegin {
            get => this.frameBegin;
            private set {
                this.frameBegin = value;
                this.InvalidateMeasure();
                this.InvalidateArrange();
                this.Track.InvalidateArrange();
            }
        }

        public long FrameDuration {
            get => this.frameDuration;
            private set {
                this.frameDuration = value;
                this.InvalidateMeasure();
                this.InvalidateArrange();
                this.Track.InvalidateArrange();
            }
        }

        public double TimelineZoom => this.Model.Track?.Timeline?.Zoom ?? 1d;
        public double PixelBegin => this.frameBegin * this.TimelineZoom;
        public double PixelWidth => this.frameDuration * this.TimelineZoom;

        public Clip Model { get; }

        private const double MinDragInitPx = 5d;
        private const double EdgeGripSize = 8d;
        public const double HeaderSize = 20;

        private long frameBegin;
        private long frameDuration;

        private DragState dragState;
        private Point clickPoint;
        private Point lastMousePos;
        private bool isUpdatingFrameSpanFromDrag;

        private readonly Binder<Clip> displayNameBinder;

        private GlyphRun glyphRun;
        private FormattedText formattedText;
        private readonly RectangleGeometry renderSizeRectGeometry;

        public TimelineClipControl(Clip clip) {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.Model = clip;
            this.displayNameBinder = Binder<Clip>.Updaters(this, DisplayNameProperty, nameof(VideoClip.DisplayNameChanged), UpdateView, UpdateModel);
            this.GotFocus += this.OnGotFocus;
            this.LostFocus += this.OnLostFocus;
            this.renderSizeRectGeometry = new RectangleGeometry();
        }

        static TimelineClipControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineClipControl), new FrameworkPropertyMetadata(typeof(TimelineClipControl)));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            this.renderSizeRectGeometry.Rect = new Rect(sizeInfo.NewSize);
        }

        private void OnGotFocus(object sender, RoutedEventArgs e) {
            Panel.SetZIndex(this, 2);
            this.Model.IsSelected = true;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e) {
            Panel.SetZIndex(this, 0);
            this.Model.IsSelected = false;
        }

        private static void UpdateView(Binder<Clip> obj) {
            ((TimelineClipControl) obj.Element).glyphRun = null;
            ((TimelineClipControl) obj.Element).formattedText = null;
            ((TimelineClipControl) obj.Element).DisplayName = obj.Model.DisplayName;
        }

        private static void UpdateModel(Binder<Clip> obj) {
            obj.Model.DisplayName = ((TimelineClipControl) obj.Element).DisplayName;
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
            this.displayNameBinder.Attach(this.Model);
        }

        public void OnRemoving() {
            this.Model.SpanChanged -= this.OnClipSpanChanged;
        }

        public void OnRemoved() {
            this.displayNameBinder.Detatch();
        }

        #endregion

        protected override void OnMouseDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);
            this.Focus();
            this.clickPoint = e.GetPosition(this);
            this.SetDragState(DragState.Initiated);
            if (!this.IsMouseCaptured)
                this.CaptureMouse();

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            if (this.dragState == DragState.Initiated) {
                this.Track.Timeline.SetPlayHeadToMouseCursor(e.MouseDevice);
            }

            this.SetDragState(DragState.None);
            this.SetCursorForMousePoint(e.GetPosition(this));
            this.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void SetDragState(DragState state) {
            if (this.dragState == state) {
                return;
            }

            this.dragState = state;
            this.SetCursorForDragState(state, false);
        }

        private void SetCursorForDragState(DragState state, bool isPreview) {
            if (isPreview && this.dragState != DragState.None) {
                return;
            }

            switch (state) {
                case DragState.None:
                    this.ClearValue(CursorProperty);
                    break;
                case DragState.Initiated:
                    break;
                case DragState.DragBody:
                    this.Cursor = Cursors.SizeAll;
                    break;
                case DragState.DragLeftEdge:
                    this.Cursor = Cursors.SizeWE;
                    break;
                case DragState.DragRightEdge:
                    this.Cursor = Cursors.SizeWE;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void SetCursorForMousePoint(Point mpos) {
            Size renderSize = this.RenderSize;
            if (mpos.X <= EdgeGripSize) {
                this.SetCursorForDragState(DragState.DragLeftEdge, true);
            }
            else if (mpos.X >= (renderSize.Width - EdgeGripSize)) {
                this.SetCursorForDragState(DragState.DragRightEdge, true);
            }
            else if (mpos.Y <= HeaderSize) {
                this.SetCursorForDragState(DragState.DragBody, true);
            }
            else {
                this.SetCursorForDragState(DragState.None, true);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (this.isUpdatingFrameSpanFromDrag) {
                // prevent possible stack overflow exceptions, at the cost of the UI possibly glitching a bit.
                // In my testing, this case is never reached, so it would require something very weird to happen
                return;
            }

            if (this.IsMovingBetweenTracks) {
                this.IsMovingBetweenTracks = false;
                return;
            }

            Point mpos = e.GetPosition(this);

            if (e.LeftButton != MouseButtonState.Pressed) {
                this.SetDragState(DragState.None);
                this.SetCursorForMousePoint(mpos);
                this.ReleaseMouseCapture();
                return;
            }

            this.SetCursorForMousePoint(mpos);
            TimelineSequenceControl timelineCtrl;
            if (this.Track == null || (timelineCtrl = this.Track.Timeline) == null) {
                return;
            }

            if (this.dragState == DragState.Initiated) {
                if (Math.Abs(mpos.X - this.clickPoint.X) < MinDragInitPx && Math.Abs(mpos.Y - this.clickPoint.Y) < MinDragInitPx) {
                    return;
                }

                if (this.clickPoint.X <= EdgeGripSize) {
                    this.SetDragState(DragState.DragLeftEdge);
                }
                else if (this.clickPoint.X >= (this.ActualWidth - EdgeGripSize)) {
                    this.SetDragState(DragState.DragRightEdge);
                }
                else if (this.clickPoint.Y <= HeaderSize) {
                    this.SetDragState(DragState.DragBody);
                }
            }

            if (this.dragState != DragState.None) {
                double zoom = this.Model.Track?.Timeline?.Zoom ?? 1.0;
                Vector mdif = mpos - this.clickPoint;
                FrameSpan oldSpan = this.Model.Span;
                if (this.dragState == DragState.DragBody) {
                    if (Math.Abs(mdif.X) >= 1.0d) {
                        long offset = (long) Math.Round(mdif.X / zoom);
                        if (offset != 0) {
                            // If begin is 2 and offset is -5, this sets offset to -2
                            // and since newBegin = begin+offset (2 + -2)
                            // this ensures begin never drops below 0
                            if ((oldSpan.Begin + offset) < 0) {
                                offset = -oldSpan.Begin;
                            }

                            if (offset != 0) {
                                FrameSpan newSpan = new FrameSpan(oldSpan.Begin + offset, oldSpan.Duration);
                                long newEndIndex = newSpan.EndIndex;
                                if (newEndIndex > timelineCtrl.Timeline.TotalFrames) {
                                    timelineCtrl.Timeline.TotalFrames = newEndIndex + 300;
                                }

                                this.isUpdatingFrameSpanFromDrag = true;
                                this.Model.Span = newSpan;
                                this.isUpdatingFrameSpanFromDrag = false;
                            }
                        }
                    }

                    if (Math.Abs(mdif.Y) >= 1.0d && timelineCtrl.Timeline is Timeline timeline) {
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
                    if (Math.Abs(mdif.X) >= 1.0d) {
                        long offset = (long) Math.Round(mdif.X / zoom);
                        if (offset != 0) {
                            if (this.dragState == DragState.DragRightEdge) {
                                // Clamps the offset to ensure we don't end up with a negative duration
                                if ((oldSpan.EndIndex + offset) <= oldSpan.Begin) {
                                    // add 1 to ensure clip is always 1 frame long, just because ;)
                                    offset = -oldSpan.Duration + 1;
                                }

                                if (offset != 0) {
                                    long newEndIndex = oldSpan.EndIndex + offset;
                                    // Clamp new frame span to 1 frame, in case user resizes too much to the right
                                    // if (newEndIndex >= oldSpan.EndIndex) {
                                    //     this.dragAccumulator -= (newEndIndex - oldSpan.EndIndex);
                                    //     newEndIndex = oldSpan.EndIndex - 1;
                                    // }

                                    FrameSpan newSpan = FrameSpan.FromIndex(oldSpan.Begin, newEndIndex);
                                    if (newEndIndex > timelineCtrl.Timeline.TotalFrames) {
                                        timelineCtrl.Timeline.TotalFrames = newEndIndex + 300;
                                    }

                                    this.isUpdatingFrameSpanFromDrag = true;
                                    this.Model.Span = newSpan;
                                    this.isUpdatingFrameSpanFromDrag = false;

                                    // account for there being no "grip" control aligned to the right side;
                                    // since the clip is resized, the origin point will not work correctly and
                                    // results in an exponential endIndex increase unless the below code is used.
                                    // This code is not needed for the left grip because it just naturally isn't
                                    this.clickPoint.X += (newSpan.EndIndex - oldSpan.EndIndex) * zoom;
                                }
                            }
                            else {
                                if ((oldSpan.Begin + offset) < 0) {
                                    offset = -oldSpan.Begin;
                                }

                                if (offset != 0) {
                                    long newBegin = oldSpan.Begin + offset;
                                    // Clamps the offset to ensure we don't end up with a negative duration
                                    if (newBegin >= oldSpan.EndIndex) {
                                        // subtract 1 to ensure clip is always 1 frame long
                                        newBegin = oldSpan.EndIndex - 1;
                                    }

                                    FrameSpan newSpan = FrameSpan.FromIndex(newBegin, oldSpan.EndIndex);
                                    long newEndIndex = newSpan.EndIndex;
                                    if (newEndIndex > timelineCtrl.Timeline.TotalFrames) {
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
        }

        public bool IsMovingBetweenTracks { get; set; }

        protected override Size MeasureOverride(Size availableSize) {
            Size size = new Size(this.PixelWidth, HeaderSize);
            base.MeasureOverride(size);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize) {
            Size size = new Size(this.PixelWidth, finalSize.Height);
            base.ArrangeOverride(size);
            return size;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);
            Rect rect = new Rect(new Point(), this.RenderSize);
            if (this.Background is Brush background) {
                dc.DrawRectangle(background, null, rect);
            }

            if (this.Track?.TrackColourBrush is Brush headerBrush) {
                dc.DrawRectangle(headerBrush, null, new Rect(0, 0, rect.Width, Math.Min(rect.Height, HeaderSize)));
            }

            // if (this.formattedText == null && this.DisplayName is string str) {
            //     Typeface typeface = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);
            //     this.formattedText = new FormattedText(str, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12d, this.Foreground, 12d);
            // }
            // if (this.formattedText != null)
            //     dc.DrawText(this.formattedText, new Point());

            if (this.glyphRun == null && this.DisplayName is string str)
                this.glyphRun = GlyphGenerator.CreateText(str, 12d, this);
            if (this.glyphRun != null) {
                dc.PushClip(this.renderSizeRectGeometry);
                dc.DrawGlyphRun(Brushes.White, this.glyphRun);
                dc.Pop();
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
