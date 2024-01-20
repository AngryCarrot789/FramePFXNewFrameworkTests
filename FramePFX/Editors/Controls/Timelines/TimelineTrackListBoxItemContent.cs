using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private readonly Binder<Track> displayNameBinder;
        private readonly Binder<Track> trackColourBinder;

        public TimelineTrackListBoxItemContent(TimelineTrackListBoxItem listItem) {
            this.ListItem = listItem;
            this.TrackColourBrush = new SolidColorBrush(Colors.Black);
            this.displayNameBinder = Binder<Track>.AutoSet(this, DisplayNameProperty, nameof(Track.DisplayNameChanged), b => b.DisplayName, (b, v) => b.DisplayName = v);
            this.trackColourBinder = Binder<Track>.Updaters(this, TrackColourBrushProperty, nameof(Track.ColourChanged), binder => {
                TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Element;
                SKColor col = element.ListItem.Track?.Colour ?? SKColors.Black;
                ((SolidColorBrush) element.TrackColourBrush).Color =  Color.FromArgb(col.Alpha, col.Red, col.Green, col.Blue);
            }, binder => {
                TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Element;
                Color col = ((SolidColorBrush) element.TrackColourBrush).Color;
                element.ListItem.Track.Colour = new SKColor(col.R, col.G, col.B, col.A);
            });
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
            this.displayNameBinder?.OnPropertyChanged(e);
            this.trackColourBinder?.OnPropertyChanged(e);
            base.OnPropertyChanged(e);
        }

        public void OnBeingAddedToTimeline() {
        }

        public void OnAddedToTimeline() {
            Track model = this.ListItem.Track;
            this.displayNameBinder.Attach(model);
            this.trackColourBinder.Attach(model);
        }

        public void OnBeginRemovedFromTimeline() {
        }

        public void OnRemovedFromTimeline() {
            this.displayNameBinder.Detatch();
            this.trackColourBinder.Detatch();
        }
    }
}