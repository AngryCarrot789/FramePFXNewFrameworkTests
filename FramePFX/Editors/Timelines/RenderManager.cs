using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;

namespace FramePFX.Editors.Timelines {
    /// <summary>
    /// A class that manages information used to draw the timeline
    /// </summary>
    public class RenderManager {
        public SKImageInfo FrameInfo { get; private set; }

        private readonly List<Task> trackRenderTasks;

        public RenderManager() {

        }

        public void UpdateFrameInfo(SKImageInfo info) {
            if (this.FrameInfo == info)
                return;
            this.FrameInfo = info;


        }
    }
}