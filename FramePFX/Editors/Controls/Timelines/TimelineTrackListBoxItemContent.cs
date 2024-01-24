using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using SkiaSharp;

namespace FramePFX.Editors.Controls.Timelines {
    public class TimelineTrackListBoxItemContent : Control {
        private static readonly Dictionary<Type, Func<TimelineTrackListBoxItemContent>> Constructors = new Dictionary<Type, Func<TimelineTrackListBoxItemContent>>();
        private static readonly DependencyPropertyKey TrackColourBrushPropertyKey = DependencyProperty.RegisterReadOnly("TrackColourBrush", typeof(Brush), typeof(TimelineTrackListBoxItemContent), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TrackColourBrushProperty = TrackColourBrushPropertyKey.DependencyProperty;
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(TimelineTrackListBoxItemContent), new PropertyMetadata(null));

        public TimelineTrackListBoxItem Owner { get; private set; }

        public Brush TrackColourBrush {
            get => (Brush) this.GetValue(TrackColourBrushProperty);
            private set => this.SetValue(TrackColourBrushPropertyKey, value);
        }

        public string DisplayName {
            get => (string) this.GetValue(DisplayNameProperty);
            set => this.SetValue(DisplayNameProperty, value);
        }

        private readonly GetSetAutoPropertyBinder<Track> displayNameBinder = new GetSetAutoPropertyBinder<Track>(DisplayNameProperty, nameof(Track.DisplayNameChanged), b => b.Model.DisplayName, (b, v) => b.Model.DisplayName = (string) v);

        private readonly AutoPropertyUpdateBinder<Track> trackColourBinder = new AutoPropertyUpdateBinder<Track>(TrackColourBrushProperty, nameof(Track.ColourChanged), binder => {
            TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Control;
            SKColor c = element.Owner.Track?.Colour ?? SKColors.Black;
            ((SolidColorBrush) element.TrackColourBrush).Color = Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
        }, binder => {
            TimelineTrackListBoxItemContent element = (TimelineTrackListBoxItemContent) binder.Control;
            Color c = ((SolidColorBrush) element.TrackColourBrush).Color;
            element.Owner.Track.Colour = new SKColor(c.R, c.G, c.B, c.A);
        });

        public TimelineTrackListBoxItemContent() {
            this.TrackColourBrush = new SolidColorBrush(Colors.Black);
        }

        public static void RegisterType<T>(Type trackType, Func<T> func) where T : TimelineTrackListBoxItemContent {
            Constructors[trackType] = func;
        }

        static TimelineTrackListBoxItemContent() {
            RegisterType(typeof(VideoTrack), () => new TimelineTrackListBoxItemContent_Video());
        }

        public static TimelineTrackListBoxItemContent NewInstance(Type trackType) {
            if (trackType == null) {
                throw new ArgumentNullException(nameof(trackType));
            }

            // Just try to find a base control type. It should be found first try unless I forgot to register a new control type
            bool hasLogged = false;
            for (Type type = trackType; type != null; type = type.BaseType) {
                if (Constructors.TryGetValue(trackType, out var func)) {
                    return func();
                }

                if (!hasLogged) {
                    hasLogged = true;
                    Debugger.Break();
                    Debug.WriteLine("Could not find control for track type on first try. Scanning base types");
                }
            }

            throw new Exception("No such content control for track type: " + trackType.Name);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!(this.GetTemplateChild("PART_DeleteTrackButton") is Button button))
                throw new Exception("Missing PART_DeleteTrackButton");

            button.Click += (sender, args) => {
                Track track = this.Owner.Track;
                track.Timeline.RemoveTrack(track);
            };
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            this.displayNameBinder?.OnPropertyChanged(e);
            this.trackColourBinder?.OnPropertyChanged(e);
        }

        public void Connect(TimelineTrackListBoxItem owner) {
            this.Owner = owner;
            this.OnConnected();
        }

        public void Disconnect() {
            this.OnDisconnected();
            this.Owner = null;
        }

        protected virtual void OnConnected() {
            Track model = this.Owner.Track;
            this.displayNameBinder.Attach(this, model);
            this.trackColourBinder.Attach(this, model);
        }

        protected virtual void OnDisconnected() {
            this.displayNameBinder.Detatch();
            this.trackColourBinder.Detatch();
        }
    }
}