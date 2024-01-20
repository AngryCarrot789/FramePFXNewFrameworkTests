using System;
using System.Diagnostics;
using System.Windows.Markup;
using SkiaSharp;

namespace FramePFX.Editors.Media.Images {
    public class ImageMediaSourceTrack : VideoMediaSourceTrack {
        public ImageMediaSource MediaSource { get; }

        public ImageMediaSourceTrack(ImageMediaSource source) {
            this.MediaSource = source;
        }

        public override void Seek(TimeSpan span) {

        }

        public override void CopyTo(SKCanvas canvas) {
            if (this.MediaSource.image != null) {
                canvas.DrawImage(this.MediaSource.image, new SKPoint());
            }
        }
    }
}