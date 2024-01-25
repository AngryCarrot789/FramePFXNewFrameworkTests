using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FramePFX.Editors.Controls.Automation;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Editors.Utils;
using FramePFX.Utils;
using SkiaSharp;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackControl : Control {
        private readonly struct MovedClip {
            public readonly TimelineClipControl control;
            public readonly Clip clip;

            public MovedClip(TimelineClipControl control, Clip clip) {
                this.control = control;
                this.clip = clip;
            }
        }

        private static readonly DependencyPropertyKey TrackColourBrushPropertyKey = DependencyProperty.RegisterReadOnly("TrackColourBrush", typeof(Brush), typeof(TimelineTrackControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TrackColourBrushProperty = TrackColourBrushPropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TimelineTrackControl), new PropertyMetadata(BoolBox.False));

        public Brush TrackColourBrush {
            get => (Brush) this.GetValue(TrackColourBrushProperty);
            private set => this.SetValue(TrackColourBrushPropertyKey, value);
        }

        public bool IsSelected {
            get => (bool) this.GetValue(IsSelectedProperty);
            set => this.SetValue(IsSelectedProperty, value.Box());
        }

        private MovedClip? clipBeingMoved;

        public TimelineSequenceControl Timeline { get; set; }

        public TimelineTrackClipPanel ClipPanel { get; private set; }

        public AutomationSequenceEditor AutomationSeqEditor { get; private set; }

        public Track Track { get; private set; }

        private readonly GetSetAutoPropertyBinder<Track> isSelectedBinder = new GetSetAutoPropertyBinder<Track>(IsSelectedProperty, nameof(VideoTrack.IsSelectedChanged), b => b.Model.IsSelected.Box(), (b, v) => b.Model.IsSelected = (bool) v);

        public TimelineTrackControl() {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.TrackColourBrush = new LinearGradientBrush();
            this.UseLayoutRounding = true;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.ClipPanel = (TimelineTrackClipPanel) this.GetTemplateChild("PART_TrackClipPanel") ?? throw new Exception("Missing PART_TrackClipPanel");
            this.ClipPanel.Track = this;
            this.AutomationSeqEditor = (AutomationSequenceEditor) this.GetTemplateChild("PART_AutomationSequenceEditor") ?? throw new Exception("Missing PART_AutomationSequenceEditor");
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseDown(e);
            if (this.Track != null) {
                if (this.Track.Timeline.HasAnySelectedTracks)
                    this.Track.Timeline.ClearTrackSelection();

                this.Track.IsSelected = true;
            }
        }

        static TimelineTrackControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelineTrackControl), new FrameworkPropertyMetadata(typeof(TimelineTrackControl)));
        }

        private void OnClipAdded(Track track, Clip clip, int index) {
            this.ClipPanel.InsertClipInternal(clip, index);
        }

        private void OnClipRemoved(Track track, Clip clip, int index) {
            this.ClipPanel.RemoveClipInternal(index);
        }

        private void OnClipMovedTracks(Clip clip, Track oldTrack, int oldIndex, Track newTrack, int newIndex) {
            if (oldTrack == this.Track) {
                TimelineTrackControl dstTrack = this.Timeline.GetTrackByModel(newTrack);
                if (dstTrack == null) {
                    // Instead of throwing, we could just remove the track or insert a new track, instead of
                    // trying to re-use existing controls, at the cost of performance.
                    // However, moving clips between tracks in different timelines is not directly supported
                    // so there's no need to support it here
                    throw new Exception("Could not find destination track. Is the UI timeline corrupted or did the clip move between timelines?");
                }

                TimelineClipControl control = (TimelineClipControl) this.ClipPanel.Clips[oldIndex];
                this.ClipPanel.RemoveClipInternal(oldIndex);
                dstTrack.clipBeingMoved = new MovedClip(control, clip);
            }
            else if (newTrack == this.Track) {
                if (!(this.clipBeingMoved is MovedClip movedClip)) {
                    throw new Exception("Clip control being moved is null. Is the UI timeline corrupted or did the clip move between timelines?");
                }

                this.ClipPanel.InsertClipInternal(movedClip.control, movedClip.clip, newIndex);
                this.clipBeingMoved = null;
            }
        }

        private void UpdateTrackColour() {
            if (this.Track == null) {
                return;
            }

            SKColor col = this.Track.Colour;
            // ((SolidColorBrush) this.TrackColourBrush).Color = Color.FromArgb(col.Alpha, col.Red, col.Green, col.Blue);

            LinearGradientBrush brush = (LinearGradientBrush) this.TrackColourBrush;
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);

            // const byte sub = 40;
            const byte sub = 80;
            Color primary = Color.FromArgb(col.Alpha, col.Red, col.Green, col.Blue);
            Color secondary = Color.FromArgb(col.Alpha, (byte) Math.Max(col.Red - sub, 0), (byte) Math.Max(col.Green - sub, 0), (byte) Math.Max(col.Blue - sub, 0));

            brush.GradientStops.Clear();
            // brush.GradientStops.Add(new GradientStop(secondary, 0.0));
            // brush.GradientStops.Add(new GradientStop(primary, 0.3));
            // brush.GradientStops.Add(new GradientStop(primary, 0.7));
            // brush.GradientStops.Add(new GradientStop(secondary, 1.0));
            brush.GradientStops.Add(new GradientStop(primary, 0.0));
            brush.GradientStops.Add(new GradientStop(secondary, 1.0));
        }

        public void OnBeingAddedToTimeline(TimelineSequenceControl parent, Track track) {
            this.Timeline = parent;
            this.Track = track;

            track.ClipAdded += this.OnClipAdded;
            track.ClipRemoved += this.OnClipRemoved;
            track.ClipMovedTracks += this.OnClipMovedTracks;
            track.HeightChanged += this.OnTrackHeightChanged;
            track.ColourChanged += this.OnTrackColourChanged;
        }

        public void OnAddedToTimeline() {
            this.isSelectedBinder.Attach(this, this.Track);
            this.UpdateTrackColour();
            int i = 0;
            foreach (Clip clip in this.Track.Clips) {
                this.ClipPanel.InsertClipInternal(clip, i++);
            }
        }

        public void OnBeginRemovedFromTimeline() {
            this.Track.ClipAdded -= this.OnClipAdded;
            this.Track.ClipRemoved -= this.OnClipRemoved;
            this.Track.ClipMovedTracks -= this.OnClipMovedTracks;
            this.Track.HeightChanged -= this.OnTrackHeightChanged;
            this.Track.ColourChanged -= this.OnTrackColourChanged;
            this.isSelectedBinder.Detatch();
            this.ClipPanel.ClearClipsInternal();
        }

        public void OnRemovedFromTimeline() {
            this.Timeline = null;
            this.Track = null;
        }

        public void OnIndexMoving(int oldIndex, int newIndex) {

        }

        public void OnIndexMoved(int oldIndex, int newIndex) {

        }

        public void OnZoomChanged(double newZoom) {
            foreach (TimelineClipControl clip in this.ClipPanel.Clips) {
                clip.OnZoomChanged(newZoom);
            }

            // this.InvalidateArrange();
        }

        private void OnTrackHeightChanged(Track track) {
            this.InvalidateMeasure();
            this.ClipPanel.InvalidateMeasure();
            this.Timeline?.InvalidateVisual();
        }

        private void OnTrackColourChanged(Track track) {
            this.UpdateTrackColour();
            this.InvalidateVisual();
            foreach (TimelineClipControl clip in this.ClipPanel.Clips) {
                clip.InvalidateVisual();
            }
        }

        public void OnClipSpanChanged() {
            this.ClipPanel.InvalidateArrange();
        }
    }
}
