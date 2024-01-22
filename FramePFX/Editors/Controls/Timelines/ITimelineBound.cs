using FramePFX.Editors.Timelines;

namespace FramePFX.Editors.Controls.Timelines {
    /// <summary>
    /// An interface for an object that exists in a timeline, somewhere
    /// </summary>
    public interface ITimelineBound {
        /// <summary>
        /// Gets the timeline associated with this object
        /// </summary>
        Timeline Timeline { get; }

        /// <summary>
        /// Gets the playhead that is relative to this object
        /// </summary>
        long RelativePlayHead { get; }
    }
}