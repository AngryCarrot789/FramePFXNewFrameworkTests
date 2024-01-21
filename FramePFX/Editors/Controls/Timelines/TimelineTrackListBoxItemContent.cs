using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using SkiaSharp;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent : Control {
        private static readonly DependencyPropertyKey TrackColourBrushPropertyKey = DependencyProperty.RegisterReadOnly("TrackColourBrush", typeof(Brush), typeof(TimelineTrackListBoxItemContent), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TrackColourBrushProperty = TrackColourBrushPropertyKey.DependencyProperty;
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(TimelineTrackListBoxItemContent), new PropertyMetadata(null));

        public Brush TrackColourBrush {
            get => (Brush) this.GetValue(TrackColourBrushProperty);
            private set => this.SetValue(TrackColourBrushPropertyKey, value);
        }

        public string DisplayName {
            get => (string) this.GetValue(DisplayNameProperty);
            set => this.SetValue(DisplayNameProperty, value);
        }

        public TimelineTrackListBoxItem ListItem { get; }

        private readonly BasicAutoBinder<Track> displayNameBinder = new BasicAutoBinder<Track>(DisplayNameProperty, nameof(Track.DisplayNameChanged), b => b.Model.DisplayName, (b, v) => b.Model.DisplayName = (string) v);

        private readonly AutoUpdaterBinder<Track> trackColourBinder = new AutoUpdaterBinder<Track>(TrackColourBrushProperty, nameof(Track.ColourChanged), binder => {
            TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Control;
            SKColor c = element.ListItem.Track?.Colour ?? SKColors.Black;
            ((SolidColorBrush) element.TrackColourBrush).Color = Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
        }, binder => {
            TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Control;
            Color c = ((SolidColorBrush) element.TrackColourBrush).Color;
            element.ListItem.Track.Colour = new SKColor(c.R, c.G, c.B, c.A);
        });

        public TimelineTrackListBoxItemContent(TimelineTrackListBoxItem listItem) {
            this.ListItem = listItem;
            this.TrackColourBrush = new SolidColorBrush(Colors.Black);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_DeleteTrackButton") is Button button))
                throw new Exception("Missing PART_DeleteTrackButton");

            button.Click += (sender, args) => {
                Track track = this.ListItem.Track;
                track.Timeline.RemoveTrack(track);
            };
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            this.displayNameBinder?.OnPropertyChanged(e);
            this.trackColourBinder?.OnPropertyChanged(e);
        }

        public virtual void OnBeingAddedToTimeline() {
        }

        public virtual void OnAddedToTimeline() {
            Track model = this.ListItem.Track;
            this.displayNameBinder.Attach(this, model);
            this.trackColourBinder.Attach(this, model);
        }

        public virtual void OnBeginRemovedFromTimeline() {
        }

        public virtual void OnRemovedFromTimeline() {
            this.displayNameBinder.Detatch();
            this.trackColourBinder.Detatch();
        }
    }
}