using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Utils;
using SkiaSharp;
using Track = FramePFX.Editors.Timelines.Tracks.Track;

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

        public ToggleButton ToggleExpandTrackButton { get; private set; }

        public ComboBox ParameterComboBox { get; private set; }

        public Button InsertKeyFrameButton { get; private set; }

        public Button ToggleOverrideButton { get; private set; }

        private double trackHeightBeforeCollapse;
        private bool ignoreTrackHeightChanged;
        private bool ignoreExpandTrackEvent;
        private readonly List<Button> actionButtons;

        private bool isProcessingParameterSelectionChanged;
        private readonly ObservableCollection<TrackListItemParameterViewModel> parameterList;
        private TrackListItemParameterViewModel selectedParameter;

        public TimelineTrackListBoxItemContent() {
            this.TrackColourBrush = new SolidColorBrush(Colors.Black);
            this.actionButtons = new List<Button>();
            this.parameterList = new ObservableCollection<TrackListItemParameterViewModel>();
        }

        protected Button CreateTrackButtonAction<T>(Button button, Action<T> action) where T : Track {
            this.actionButtons.Add(button);
            button.Click += (sender, args) => {
                Track track = this.Owner?.Track;
                if (track is T)
                    action((T) track);
            };

            return button;
        }

        protected Button CreateBasicButtonAction(Button button, Action action) {
            this.actionButtons.Add(button);
            button.Click += (sender, args) => {
                if (this.Owner != null)
                    action();
            };

            return button;
        }

        protected void GetTemplateChild<T>(string name, out T value) where T : DependencyObject {
            if ((value = this.GetTemplateChild(name) as T) == null)
                throw new Exception("Missing part: " + name);
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
            this.GetTemplateChild("PART_DeleteTrackButton", out Button delTrackButton);
            this.GetTemplateChild("PART_ExpandTrackButton", out ToggleButton expandButton);
            this.GetTemplateChild("PART_InsertKeyFrameButton", out Button insertKeyFrameButton);
            this.GetTemplateChild("PART_OverrideButton", out Button toggleOverrideButton);
            this.GetTemplateChild("PART_ParameterComboBox", out ComboBox paramComboBox);

            delTrackButton.Click += (sender, args) => {
                Track track = this.Owner.Track;
                track.Timeline.RemoveTrack(track);
            };

            this.ToggleExpandTrackButton = expandButton;
            expandButton.IsThreeState = false;
            expandButton.Checked += this.ExpandTrackCheckedChanged;
            expandButton.Unchecked += this.ExpandTrackCheckedChanged;

            this.InsertKeyFrameButton = insertKeyFrameButton;
            this.CreateBasicButtonAction(insertKeyFrameButton, () => {
                if (this.selectedParameter != null) {
                    Track track = this.Owner.Track;
                    long frame = track.RelativePlayHead;
                    AutomationSequence seq = track.AutomationData[this.selectedParameter.parameter];
                    seq.AddNewKeyFrame(frame, out KeyFrame keyFrame);
                    keyFrame.AssignCurrentValue(frame, seq);
                }
            });

            this.ToggleOverrideButton = insertKeyFrameButton;
            this.CreateBasicButtonAction(toggleOverrideButton, () => {
                if (this.selectedParameter != null) {
                    AutomationSequence seq = this.Owner.Track.AutomationData[this.selectedParameter.parameter];
                    seq.IsOverrideEnabled = !seq.IsOverrideEnabled;
                }
            });

            paramComboBox.ItemsSource = this.parameterList;
            paramComboBox.SelectionChanged += this.OnParameterSelectionChanged;
            this.ParameterComboBox = paramComboBox;
            this.UpdateForSelectedParameter();
        }

        private void OnParameterSelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.isProcessingParameterSelectionChanged = true;
            TrackListItemParameterViewModel oldItem = this.selectedParameter;
            this.selectedParameter = this.ParameterComboBox.SelectedItem as TrackListItemParameterViewModel;
            this.OnSelectedParameterChanged(oldItem, this.selectedParameter);
            this.isProcessingParameterSelectionChanged = false;
        }

        private void OnSelectedParameterChanged(TrackListItemParameterViewModel oldItem, TrackListItemParameterViewModel newItem) {
            this.UpdateForSelectedParameter();
        }

        private void UpdateForSelectedParameter() {
            this.InsertKeyFrameButton.IsEnabled = this.selectedParameter != null;
            this.ToggleOverrideButton.IsEnabled = this.selectedParameter != null;
        }

        private void ExpandTrackCheckedChanged(object sender, RoutedEventArgs e) {
            if (this.ignoreExpandTrackEvent)
                return;

            bool isExpanded = this.ToggleExpandTrackButton.IsChecked ?? false;
            Track track = this.Owner.Track;
            this.ignoreTrackHeightChanged = true;

            if (isExpanded) {
                track.Height = this.trackHeightBeforeCollapse;
            }
            else {
                this.trackHeightBeforeCollapse = track.Height;
                track.Height = Track.MinimumHeight;
            }

            this.ignoreTrackHeightChanged = false;
        }

        private void OnTrackHeightChanged(Track track) {
            if (this.ignoreTrackHeightChanged)
                return;
            this.UpdateTrackHeightExpander();
        }

        private void UpdateTrackHeightExpander() {
            this.ignoreExpandTrackEvent = true;
            this.ToggleExpandTrackButton.IsChecked = !Maths.Equals(this.Owner.Track.Height, Track.MinimumHeight);
            this.ignoreExpandTrackEvent = false;
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
            Track track = this.Owner.Track;
            this.displayNameBinder.Attach(this, track);
            this.trackColourBinder.Attach(this, track);
            track.HeightChanged += this.OnTrackHeightChanged;
            this.UpdateTrackHeightExpander();

            foreach (Parameter parameter in Parameter.GetApplicableParameters(track.GetType())) {
                this.parameterList.Add(new TrackListItemParameterViewModel(this, parameter));
            }
        }

        protected virtual void OnDisconnected() {
            this.displayNameBinder.Detatch();
            this.trackColourBinder.Detatch();
            this.Owner.Track.HeightChanged -= this.OnTrackHeightChanged;
            this.parameterList.Clear();
        }
    }

    public class TrackListItemParameterViewModel : INotifyPropertyChanged {
        internal readonly Parameter parameter;

        public string Name => this.parameter.Key.Name;

        public string FullName => this.parameter.Key.ToString();

        public event PropertyChangedEventHandler PropertyChanged;

        public TrackListItemParameterViewModel() {
        }

        public TrackListItemParameterViewModel(TimelineTrackListBoxItemContent owner, Parameter parameter) {
            this.parameter = parameter;
        }

        public void Connect() {

        }

        public void Disconnect() {

        }

        public override string ToString() {
            return this.Name;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}