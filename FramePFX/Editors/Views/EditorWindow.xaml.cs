using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Views;
using SkiaSharp;

namespace FramePFX.Editors.Views {
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : WindowEx {
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register("Editor", typeof(VideoEditor), typeof(EditorWindow), new PropertyMetadata(null, (o, e) => ((EditorWindow) o).OnEditorChanged((VideoEditor)e.OldValue, (VideoEditor)e.NewValue)));

        public VideoEditor Editor {
            get => (VideoEditor) this.GetValue(EditorProperty);
            set => this.SetValue(EditorProperty, value);
        }

        public EditorWindow() {
            this.InitializeComponent();
            this.Loaded += this.EditorWindow_Loaded;
        }

        private void EditorWindow_Loaded(object sender, RoutedEventArgs e) {
            if (this.ViewPortElement.BeginRender(out SKSurface surface)) {
                using (SKPaint paint = new SKPaint() { Color = SKColors.Black }) {
                    surface.Canvas.DrawRect(0, 0, 1280, 720, paint);
                }

                using (SKPaint paint = new SKPaint() { Color = SKColors.OrangeRed }) {
                    surface.Canvas.DrawRect(0, 0, 90, 30, paint);
                }

                this.ViewPortElement.EndRender();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.Key == Key.S) {
                Timeline timeline = this.Editor?.CurrentProject?.MainTimeline;
                if (timeline == null) {
                    return;
                }

                Clip selected = null;
                foreach (Track track in timeline.Tracks) {
                    if ((selected = track.Clips.FirstOrDefault(x => x.IsSelected)) != null) {
                        break;
                    }
                }

                long playHead = timeline.PlayHeadPosition;
                if (selected != null && selected.IsPlayHeadIntersecting(playHead) && playHead != selected.Span.Begin && playHead != selected.Span.EndIndex) {
                    selected.CutAt(playHead - selected.Span.Begin);
                }
            }
        }

        private void OnEditorChanged(VideoEditor oldEditor, VideoEditor newEditor) {
            if (oldEditor != null) {
                oldEditor.ProjectChanged -= this.OnEditorProjectChanged;
            }

            if (newEditor != null) {
                newEditor.ProjectChanged += this.OnEditorProjectChanged;
                if (newEditor.CurrentProject != null) {
                    this.OnCurrentTimelineChanged(newEditor.CurrentProject.MainTimeline);
                }
            }
        }

        private void OnEditorProjectChanged(VideoEditor editor, Project oldProject, Project newProject) {
            this.OnCurrentTimelineChanged(newProject?.MainTimeline);
        }

        private void OnCurrentTimelineChanged(Timeline timeline) {
            this.TheTimeline.Timeline = timeline;
        }
    }
}
