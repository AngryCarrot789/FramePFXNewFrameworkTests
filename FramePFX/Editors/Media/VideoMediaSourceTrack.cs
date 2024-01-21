using System;
using SkiaSharp;

namespace FramePFX.Editors.Media {
    public abstract class VideoMediaSourceTrack : MediaSourceTrack {
        public abstract void Seek(TimeSpan span);

        public abstract void CopyTo(SKCanvas canvas);
    }
}