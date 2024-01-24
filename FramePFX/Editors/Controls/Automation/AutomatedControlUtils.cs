using System.Windows;
using FramePFX.Editors.Automation;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls.Automation {
    public class AutomatedControlUtils {
        public static void SetDefaultKeyFrameOrAddNew(IAutomatable automatable, DependencyObject control, Parameter parameter, DependencyProperty property) {
            AutomationSequence sequence = automatable.AutomationData[parameter];
            if (sequence.IsEmpty || sequence.IsOverrideEnabled) {
                sequence.DefaultKeyFrame.SetValueFromObject(control.GetValue(property));
            }
            else {
                long frame = automatable.RelativePlayHead;
                if (automatable is IStrictFrameRange && !((IStrictFrameRange) automatable).IsRelativeFrameInRange(frame)) {
                    // when the object is has a strict frame range, e.g. clip, effect, and it is not in range,
                    // enable override and set the default key frame
                    sequence.DefaultKeyFrame.SetValueFromObject(control.GetValue(property));
                    sequence.IsOverrideEnabled = true;
                }
                else {
                    // Either get the last key frame at the playhead or create a new one at that location
                    int index = sequence.GetLastFrameExactlyAt(frame);
                    KeyFrame keyFrame;
                    if (index == -1) {
                        index = sequence.AddNewKeyFrame(frame, out keyFrame);
                    }
                    else {
                        keyFrame = sequence.GetKeyFrameAtIndex(index);
                    }

                    keyFrame.SetValueFromObject(control.GetValue(property));
                }
            }
        }
    }
}