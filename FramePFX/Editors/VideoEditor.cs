using System;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors {
    public delegate void ProjectChangedEventHandler(VideoEditor editor, Project oldProject, Project newProject);

    /// <summary>
    /// The class which stores all of the data for the video editor application
    /// </summary>
    public class VideoEditor {
        public Project CurrentProject { get; private set; }

        public event ProjectChangedEventHandler ProjectChanged;

        public VideoEditor() {

        }

        public void LoadDefaultProject() {
            if (this.CurrentProject != null) {
                throw new Exception("A project is already loaded");
            }

            Project project = new Project();

            {
                Track track = new VideoTrack() {DisplayName = "Vid Track 1"};
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(0, 100), DisplayName = "Clip 1"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(150, 100), DisplayName = "Clip 2"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(300, 250), DisplayName = "Clip 3"});
                project.MainTimeline.AddTrack(track);
            }

            {
                Track track = new VideoTrack() {DisplayName = "Vid Track 2"};
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(100, 50), DisplayName = "Clip 4"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(150, 200), DisplayName = "Clip 5"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(500, 125), DisplayName = "Clip 6"});
                project.MainTimeline.AddTrack(track);
            }

            {
                Track track = new VideoTrack() {DisplayName = "Vid Track 3!!"};
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(20, 80), DisplayName = "Clip 7"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(150, 100), DisplayName = "Clip 8"});
                track.AddClip(new VideoClip() {FrameSpan = new FrameSpan(350, 200), DisplayName = "Clip 9"});
                project.MainTimeline.AddTrack(track);
            }

            this.SetProject(project);
        }

        public void SetProject(Project project) {
            if (this.CurrentProject != null) {
                throw new Exception("A project is already loaded; it must be unloaded first");
            }

            this.CurrentProject = project;
            Project.OnOpened(this, project);
            this.ProjectChanged?.Invoke(this, null, project);
        }

        public void CloseProject() {
            Project oldProject = this.CurrentProject;
            if (oldProject == null) {
                throw new Exception("There is no project opened");
            }

            oldProject.Destroy();
            this.CurrentProject = null;
            Project.OnClosed(this, oldProject);
            this.ProjectChanged?.Invoke(this, oldProject, null);
        }
    }
}