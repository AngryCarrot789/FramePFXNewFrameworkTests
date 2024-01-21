using System;
using FramePFX.Editors.ResourceManaging;
using FramePFX.Editors.ResourceManaging.Resources;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors {
    public delegate void ProjectChangedEventHandler(VideoEditor editor, Project oldProject, Project newProject);

    /// <summary>
    /// The class which stores all of the data for the video editor application
    /// </summary>
    public class VideoEditor {
        /// <summary>
        /// Gets the project that is currently loaded
        /// </summary>
        public Project Project { get; private set; }

        public PlaybackManager Playback { get; }

        public event ProjectChangedEventHandler ProjectChanged;

        public VideoEditor() {
            this.Playback = new PlaybackManager(this);
            this.Playback.SetFrameRate(60.0);
            this.Playback.StartTimer();
        }

        public void LoadDefaultProject() {
            if (this.Project != null) {
                throw new Exception("A project is already loaded");
            }

            Project project = new Project();

            ResourceManager manager = project.ResourceManager;
            ulong id_r = manager.RegisterEntry(manager.RootFolder.AddItemAndRet(new ResourceColour(220, 25, 25) {DisplayName = "colour_red"}));
            ulong id_g = manager.RegisterEntry(manager.RootFolder.AddItemAndRet(new ResourceColour(25, 220, 25) {DisplayName = "colour_green"}));
            ulong id_b = manager.RegisterEntry(manager.RootFolder.AddItemAndRet(new ResourceColour(25, 25, 220) {DisplayName = "colour_blue"}));

            ResourceFolder folder = new ResourceFolder("Extra Colours");
            manager.RootFolder.AddItem(folder);
            ulong id_w = manager.RegisterEntry(folder.AddItemAndRet(new ResourceColour(220, 220, 220) {DisplayName = "white colour"}));
            ulong id_d = manager.RegisterEntry(folder.AddItemAndRet(new ResourceColour(50, 100, 220) {DisplayName = "idek"}));


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
            if (this.Project != null) {
                throw new Exception("A project is already loaded; it must be unloaded first");
            }

            this.Project = project;
            Project.OnOpened(this, project);
            this.ProjectChanged?.Invoke(this, null, project);
        }

        public void CloseProject() {
            Project oldProject = this.Project;
            if (oldProject == null) {
                throw new Exception("There is no project opened");
            }

            oldProject.Destroy();
            this.Project = null;
            Project.OnClosed(this, oldProject);
            this.ProjectChanged?.Invoke(this, oldProject, null);
        }
    }
}