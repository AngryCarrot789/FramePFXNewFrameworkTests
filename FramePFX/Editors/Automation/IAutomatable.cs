using FramePFX.Editors.Controls.Timelines;

namespace FramePFX.Editors.Automation {
    /// <summary>
    /// An interface implemented by an object which supports parameter automation
    /// </summary>
    public interface IAutomatable : IHasTimeline {
        /// <summary>
        /// The automation data for this object, which stores a collection of automation
        /// sequences for storing the key frames for each type of automate-able parameters
        /// </summary>
        AutomationData AutomationData { get; }
    }
}