using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using Expression = System.Linq.Expressions.Expression;

namespace FramePFX.Editors.Controls {
    public class TimelineTrackControl : Panel {
        private static readonly Random RANDOM_HEADER = new Random();
        private TimelineClipControl clipBeingMoved;

        public TimelineControl Timeline { get; set; }

        public Track Track { get; }

        public Brush ClipHeaderColour { get; }

        public TimelineTrackControl(Track track) {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Top;

            this.Track = track;
            this.ClipHeaderColour = new SolidColorBrush(Color.FromArgb(255, (byte)RANDOM_HEADER.Next(256), (byte)RANDOM_HEADER.Next(256), (byte)RANDOM_HEADER.Next(256)));
            this.ClipHeaderColour.Freeze();

            this.Background = SystemColors.ControlBrush;
            this.UseLayoutRounding = true;
        }

        private void OnClipAdded(Track track, Clip clip, int index) {
            this.InsertClipInternal(clip, index);
        }

        private void OnClipRemoved(Track track, Clip clip, int index) {
            this.RemoveClipInternal(clip, index);
        }

        private void OnClipMovedTracks(Clip clip, Track oldTrack, int oldIndex, Track newTrack, int newIndex) {
            if (oldTrack == this.Track) {
                TimelineTrackControl dstTrack = this.Timeline.GetTrackByModel(newTrack);
                if (dstTrack == null) {
                    throw new Exception("Could not find destination track. Is the UI timeline corrupted or did the clip move between timelines?");
                }

                TimelineClipControl control = (TimelineClipControl) this.InternalChildren[oldIndex];
                this.RemoveClipInternal(clip, oldIndex);
                dstTrack.clipBeingMoved = control;
            }
            else if (newTrack == this.Track) {
                if (this.clipBeingMoved == null) {
                    throw new Exception("Clip control being moved is null. Is the UI timeline corrupted or did the clip move between timelines?");
                }

                this.InsertClipInternal(this.clipBeingMoved, newIndex);
                this.clipBeingMoved = null;
            }
        }

        private void InsertClipInternal(Clip clip, int index) {
            TimelineClipControl control = new TimelineClipControl(clip);
            this.InsertClipInternal(control, index);
        }

        private void InsertClipInternal(TimelineClipControl control, int index) {
            control.Track = this;
            control.OnAdding();
            this.InternalChildren.Insert(index, control);
            control.OnAdded();
        }

        private void RemoveClipInternal(Clip clip, int index) {
            TimelineClipControl control = (TimelineClipControl) this.InternalChildren[index];
            control.OnRemoving();
            this.InternalChildren.RemoveAt(index);
            control.OnRemoved();
            control.Track = null;
        }

        protected override Size MeasureOverride(Size availableSize) {
            availableSize.Height = this.Track.Height;
            Size total = new Size();
            UIElementCollection items = this.InternalChildren;
            int count = items.Count;
            for (int i = 0; i < count; i++) {
                UIElement item = items[i];
                item.Measure(availableSize);
                Size size = item.DesiredSize;
                total.Width = Math.Max(total.Width, size.Width);
                total.Height = Math.Max(total.Height, size.Height);
            }

            return new Size(total.Width, availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            UIElementCollection items = this.InternalChildren;
            for (int i = 0, count = items.Count; i < count; i++) {
                TimelineClipControl clip = (TimelineClipControl) items[i];
                clip.Arrange(new Rect(clip.PixelBegin, 0, clip.PixelWidth, finalSize.Height));
            }

            return finalSize;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);
            // Size size = this.RenderSize;
            // Pen pen = new Pen(Brushes.Black, 1d);
            // dc.DrawLine(pen, new Point(0, -1), new Point(size.Width, -1));
            // dc.DrawLine(pen, new Point(0, size.Height), new Point(size.Width, size.Height));
        }

        public void OnBeingAddedToTimeline() {
            this.Track.ClipAdded += this.OnClipAdded;
            this.Track.ClipRemoved += this.OnClipRemoved;
            this.Track.ClipMovedTracks += this.OnClipMovedTracks;
            this.Track.HeightChanged += this.OnTrackHeightChanged;
            int i = 0;
            foreach (Clip clip in this.Track.Clips) {
                this.InsertClipInternal(clip, i++);
            }
        }

        public void OnAddedToTimeline() {

        }

        public void OnBeginRemovedFromTimeline() {
            this.Track.ClipAdded -= this.OnClipAdded;
            this.Track.ClipRemoved -= this.OnClipRemoved;
            this.Track.ClipMovedTracks -= this.OnClipMovedTracks;
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
        }

        private void OnTrackHeightChanged(Track track) {
            this.InvalidateMeasure();
        }

        public void OnRemovedFromTimeline() {

        }

        public void OnIndexMoving(int oldIndex, int newIndex) {

        }

        public void OnIndexMoved(int oldIndex, int newIndex) {

        }

        public void OnZoomChanged(double newZoom) {
            foreach (TimelineClipControl clip in this.InternalChildren) {
                clip.OnZoomChanged(newZoom);
            }

            this.InvalidateArrange();
        }
    }
}
