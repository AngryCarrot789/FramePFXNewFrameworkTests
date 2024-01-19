using FramePFX.Destroying;
using FramePFX.Editors.Timelines;

namespace FramePFX.Editors {
    public class Project : IDestroy {
        public Timeline MainTimeline { get; }

        public VideoEditor Editor { get; private set; }

        public Project() {
            this.MainTimeline = new Timeline();
            Timeline.SetMainTimelineProjectReference(this.MainTimeline, this);
        }

        /// <summary>
        /// Destroys all of this project's resources, timeline, tracks, clips, etc., allowing for it to be safely garbage collected.
        /// This is called when closing a project, or loading a new project (old project destroyed, new one is loaded)
        /// </summary>
        public void Destroy() {
            this.MainTimeline.Destroy();
        }

        internal static void OnOpened(VideoEditor editor, Project project) {
            project.Editor = editor;
        }

        internal static void OnClosed(VideoEditor editor, Project project) {
            project.Editor = null;
        }
    }
}