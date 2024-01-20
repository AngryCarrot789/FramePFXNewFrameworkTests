using System.ComponentModel;
using System.Windows;
using FramePFX.Editors.Timelines;
using FramePFX.Views;

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
