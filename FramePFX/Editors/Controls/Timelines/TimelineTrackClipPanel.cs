using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackClipPanel : Panel {
        public TimelineTrackControl Track { get; set; }

        private readonly Stack<TimelineClipControl> itemCache;

        public UIElementCollection Clips => this.InternalChildren;

        public TimelineTrackClipPanel() {
            this.itemCache = new Stack<TimelineClipControl>();
        }

        public void InsertClipInternal(Clip clip, int index) {
            this.InsertClipInternal(this.itemCache.Count > 0 ? this.itemCache.Pop() : new TimelineClipControl(), clip, index);
        }

        public void InsertClipInternal(TimelineClipControl control, Clip clip, int index) {
            if (this.Track == null)
                throw new InvalidOperationException("Cannot insert clips without a track associated");
            control.OnAdding(this.Track, clip);
            this.InternalChildren.Insert(index, control);
            // control.InvalidateMeasure();
            // control.UpdateLayout();
            control.ApplyTemplate();
            control.OnAdded();
        }

        public void RemoveClipInternal(int index) {
            TimelineClipControl control = (TimelineClipControl) this.InternalChildren[index];
            control.OnRemoving();
            this.InternalChildren.RemoveAt(index);
            control.OnRemoved();
            if (this.itemCache.Count < 16)
                this.itemCache.Push(control);
        }

        public void ClearClipsInternal() {
            int count = this.InternalChildren.Count;
            for (int i = count - 1; i >= 0; i--) {
                this.RemoveClipInternal(i);
            }
        }

        protected override Size MeasureOverride(Size availableSize) {
            if (this.Track != null) {
                availableSize.Height = this.Track.Track.Height;
            }

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
    }
}