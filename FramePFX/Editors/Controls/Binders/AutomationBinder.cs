using System;
using System.Windows;
using FramePFX.Editors.Automation;
using FramePFX.Editors.Automation.Keyframes;
using FramePFX.Editors.Automation.Params;
using FramePFX.Editors.Controls.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;

namespace FramePFX.Editors.Controls.Binders {
    public class AutomationBinder<TModel> : BaseObjectBinder<TModel> where TModel : class, ITimelineBound, IAutomatable {
        private readonly ParameterChangedEventHandler handler;
        private readonly Parameter parameter;
        private readonly DependencyProperty property;

        public AutomationBinder(Parameter parameter, DependencyProperty property) {
            this.handler = this.OnParameterValueChanged;
            this.parameter = parameter;
            this.property = property;
        }

        private void OnParameterValueChanged(AutomationSequence sequence) {
            this.OnModelValueChanged();
        }

        protected override void OnAttached() {
            base.OnAttached();
            this.Model.AutomationData.AddParameterChangedHandler(this.parameter, this.handler);
        }

        protected override void OnDetatched() {
            base.OnDetatched();
            this.Model.AutomationData.RemoveParameterChangedHandler(this.parameter, this.handler);
        }

        protected override void UpdateModelCore() {
            AutomationSequence sequence = this.Model.AutomationData[this.parameter];
            if (sequence.IsEmpty || sequence.IsOverrideEnabled) {
                object value = this.Control.GetValue(this.property);
                sequence.DefaultKeyFrame.SetValueFromObject(value);
            }
            else {
                long frame = this.Model.RelativePlayHead;
                int index = sequence.GetLastFrameExactlyAt(frame);
                KeyFrame keyFrame;
                if (index == -1) {
                    index = sequence.AddNewKeyFrame(frame, out keyFrame);
                }
                else {
                    keyFrame = sequence.GetKeyFrameAtIndex(index);
                }

                // keyFrame.AssignCurrentValue(frame, sequence, true);
                keyFrame.SetValueFromObject(this.Control.GetValue(this.property));
            }

            if (this.Model is VideoClip || this.Model is VideoTrack) {
                this.Model.Timeline?.Project?.RenderManager.InvalidateRender();
            }
        }

        protected override void UpdateControlCore() {
            object value;
            switch (this.parameter.DataType) {
                case AutomationDataType.Float: value = ((ParameterFloat) this.parameter).GetEffectiveValue(this.Model); break;
                case AutomationDataType.Double: value = ((ParameterDouble) this.parameter).GetEffectiveValue(this.Model); break;
                case AutomationDataType.Long: value = ((ParameterLong) this.parameter).GetEffectiveValue(this.Model); break;
                case AutomationDataType.Boolean: value = ((ParameterBoolean) this.parameter).GetEffectiveValue(this.Model); break;
                default: throw new ArgumentOutOfRangeException();
            }

            this.Control.SetValue(this.property, value);
        }
    }
}