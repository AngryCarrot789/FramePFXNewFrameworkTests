using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FramePFX.Editors.Controls.Resources;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Utils;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBox : ListBox {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineTrackListBox), new PropertyMetadata(null, (d, e) => ((TimelineTrackListBox) d).OnTimelineChanged((Timeline) e.OldValue, (Timeline) e.NewValue)));

        public Timeline Timeline {
            get => (Timeline) this.GetValue(TimelineProperty);
            set => this.SetValue(TimelineProperty, value);
        }

        private const int MaxItemCacheCount = 8;
        private const int MaxItemContentCacheCount = 4;
        private readonly Stack<TimelineTrackListBoxItem> cachedItems;
        private readonly Dictionary<Type, Stack<TimelineTrackListBoxItemContent>> itemContentCacheMap;

        public TimelineTrackListBox() {
            this.cachedItems = new Stack<TimelineTrackListBoxItem>();
            this.itemContentCacheMap = new Dictionary<Type, Stack<TimelineTrackListBoxItemContent>>();
            this.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(TimelineTrackListBoxItemPanel)));
            this.SelectionMode = SelectionMode.Extended;
        }

        static TimelineTrackListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (TimelineTrackListBox), new FrameworkPropertyMetadata(typeof(TimelineTrackListBox)));
        }

        // I override the measuere/arrange functions to help with debugging sometimes

        protected override Size MeasureOverride(Size constraint) {
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds) {
            return base.ArrangeOverride(arrangeBounds);
        }

        private void OnTimelineChanged(Timeline oldTimeline, Timeline newTimeline) {
            if (oldTimeline == newTimeline)
                return;
            if (oldTimeline != null) {
                oldTimeline.TrackAdded -= this.OnTrackAdded;
                oldTimeline.TrackRemoved -= this.OnTrackRemoved;
                oldTimeline.TrackMoved -= this.OnTrackMoved;
                for (int i = this.Items.Count - 1; i >= 0; i--) {
                    this.RemoveTrackInternal(i);
                }
            }

            if (newTimeline != null) {
                newTimeline.TrackAdded += this.OnTrackAdded;
                newTimeline.TrackRemoved += this.OnTrackRemoved;
                newTimeline.TrackMoved += this.OnTrackMoved;

                int i = 0;
                foreach (Track track in newTimeline.Tracks) {
                    this.InsertTrackInternal(track, i++);
                }
            }
        }

        private void OnTrackAdded(Timeline timeline, Track track, int index) {
            this.InsertTrackInternal(track, index);
        }

        private void OnTrackRemoved(Timeline timeline, Track track, int index) {
            this.RemoveTrackInternal(index);
        }

        private void InsertTrackInternal(Track track, int index) {
            TimelineTrackListBoxItem control = this.cachedItems.Count > 0 ? this.cachedItems.Pop() : new TimelineTrackListBoxItem();
            control.OnAddingToList(this, track);
            this.Items.Insert(index, control);
            // UpdateLayout must be called explicitly, so that the visual tree
            // can be measured, allowing templates to be applied
            control.UpdateLayout();
            control.OnAddedToList();
            control.InvalidateMeasure();
            this.InvalidateMeasure();
        }

        private void RemoveTrackInternal(int index) {
            TimelineTrackListBoxItem control = (TimelineTrackListBoxItem) this.Items[index];
            control.OnRemovingFromList();
            this.Items.RemoveAt(index);
            control.OnRemovedFromList();
            this.cachedItems.Push(control);
            this.InvalidateMeasure();
        }

        private void OnTrackMoved(Timeline timeline, Track track, int oldIndex, int newIndex) {
            TimelineTrackListBoxItem control = (TimelineTrackListBoxItem) this.Items[oldIndex];
            control.OnIndexMoving(oldIndex, newIndex);
            this.Items.RemoveAt(oldIndex);
            this.Items.Insert(newIndex, control);
            control.OnIndexMoved(oldIndex, newIndex);
            this.InvalidateMeasure();
        }

        public TimelineTrackListBoxItemContent GetContentObject(Type trackType) {
            TimelineTrackListBoxItemContent content;
            if (this.itemContentCacheMap.TryGetValue(trackType, out Stack<TimelineTrackListBoxItemContent> stack) && stack.Count > 0) {
                content = stack.Pop();
            }
            else {
                content = TimelineTrackListBoxItemContent.NewInstance(trackType);
            }

            return content;
        }

        public bool ReleaseContentObject(Type trackType, TimelineTrackListBoxItemContent contentControl) {
            if (!this.itemContentCacheMap.TryGetValue(trackType, out Stack<TimelineTrackListBoxItemContent> stack)) {
                this.itemContentCacheMap[trackType] = stack = new Stack<TimelineTrackListBoxItemContent>();
            }
            else if (stack.Count > MaxItemContentCacheCount) {
                return false;
            }

            stack.Push(contentControl);
            return true;
        }
    }
}