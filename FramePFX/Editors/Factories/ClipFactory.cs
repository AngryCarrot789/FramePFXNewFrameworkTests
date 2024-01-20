using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Factories {
    public class ClipFactory : ReflectiveObjectFactory<Clip> {
        public static ClipFactory Instance { get; } = new ClipFactory();

        private ClipFactory() {
            this.RegisterType("clip_vid", typeof(VideoClip));
        }

        public Clip NewClip(string id) {
            return base.NewInstance(id);
        }
    }
}