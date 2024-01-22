using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FramePFX.Editors.Automation.Keyframes;

namespace FramePFX.Editors.Automation.Params {
    /// <summary>
    /// A class that stores information about a registered parameter for a specific type of automatable object
    /// </summary>
    public abstract class Parameter {
        private static readonly Dictionary<ParameterKey, Parameter> RegistryMap;
        private static readonly Dictionary<Type, List<Parameter>> TypeToParametersMap;
        private static volatile int RegistrationFlag;
        private static volatile int NextGlobalIndex;

        public const string FullIdSplitter = "::";

        /// <summary>
        /// This parameter's unique key that identifies it globally
        /// </summary>
        public ParameterKey Key { get; }

        /// <summary>
        /// Gets the class type that owns this parameter
        /// </summary>
        public Type OwnerType { get; }

        /// <summary>
        /// The automation data type of this parameter
        /// </summary>
        public AutomationDataType DataType { get; }

        /// <summary>
        /// Gets this parameter's descriptor
        /// </summary>
        public ParameterDescriptor Descriptor { get; }

        public int GlobalIndex { get; private set; }

        protected Parameter(Type ownerType, ParameterKey key, ParameterDescriptor descriptor) {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (ownerType == null)
                throw new ArgumentNullException(nameof(ownerType));
            this.Key = key;
            this.OwnerType = ownerType;
            this.Descriptor = descriptor;
            this.DataType = descriptor.DataType;
        }

        static Parameter() {
            RegistryMap = new Dictionary<ParameterKey, Parameter>();
            TypeToParametersMap = new Dictionary<Type, List<Parameter>>();
        }

        /// <summary>
        /// Calculates and sets effective value of the sequence's data owner. Calling this method directly will
        /// not result in any events being fired, therefore, this method shouldn't really be called directly.
        /// Instead, use <see cref="AutomationSequence.UpdateValue"/> which fires the appropriate sequence of events
        /// </summary>
        /// <param name="sequence">The sequence used to reference the parameter and automation data owner</param>
        /// <param name="frame">The frame which should be used to calculate the new effective value</param>
        public abstract void SetValue(AutomationSequence sequence, long frame);

        public KeyFrame CreateKeyFrame(long frame = 0L) => KeyFrame.CreateDefault(this, frame);

        #region Static Stuff

        public static ParameterFloat RegisterFloat(Type ownerType, string domain, string parameterName, ParameterDescriptorFloat descriptor, Func<IAutomatable, float> getter, Action<IAutomatable, float> setter) {
            return (ParameterFloat) RegisterInternal(new ParameterFloat(ownerType, new ParameterKey(domain, parameterName), descriptor, getter, setter));
        }

        public static ParameterDouble RegisterDouble(Type ownerType, string domain, string parameterName, ParameterDescriptorDouble descriptor, Func<IAutomatable, double> getter, Action<IAutomatable, double> setter) {
            return (ParameterDouble) RegisterInternal(new ParameterDouble(ownerType, new ParameterKey(domain, parameterName), descriptor, getter, setter));
        }

        public static ParameterLong RegisterLong(Type ownerType, string domain, string parameterName, ParameterDescriptorLong descriptor, Func<IAutomatable, long> getter, Action<IAutomatable, long> setter) {
            return (ParameterLong) RegisterInternal(new ParameterLong(ownerType, new ParameterKey(domain, parameterName), descriptor, getter, setter));
        }

        public static ParameterBoolean RegisterBoolean(Type ownerType, string domain, string parameterName, ParameterDescriptorBoolean descriptor, Func<IAutomatable, bool> getter, Action<IAutomatable, bool> setter) {
            return (ParameterBoolean) RegisterInternal(new ParameterBoolean(ownerType, new ParameterKey(domain, parameterName), descriptor, getter, setter));
        }

        public static Parameter GetParameterByKey(ParameterKey key, Parameter def = null) {
            return TryGetParameterByKey(key, out Parameter parameter) ? parameter : def;
        }

        public static bool TryGetParameterByKey(ParameterKey key, out Parameter parameter) {
            while (Interlocked.CompareExchange(ref RegistrationFlag, 2, 0) != 0)
                Thread.Sleep(1);

            try {
                return RegistryMap.TryGetValue(key, out parameter);
            }
            finally {
                RegistrationFlag = 0;
            }
        }

        private static Parameter RegisterInternal(Parameter parameter) {
            ParameterKey path = parameter.Key;
            while (Interlocked.CompareExchange(ref RegistrationFlag, 1, 0) != 0)
                Thread.SpinWait(32);

            try {
                if (RegistryMap.TryGetValue(path, out Parameter existingParameter)) {
                    throw new Exception($"Key already exists with the ID '{path}': {existingParameter}");
                }

                RegistryMap[path] = parameter;
                if (!TypeToParametersMap.TryGetValue(parameter.OwnerType, out List<Parameter> list))
                    TypeToParametersMap[parameter.OwnerType] = list = new List<Parameter>();
                list.Add(parameter);
                parameter.GlobalIndex = NextGlobalIndex;
                Interlocked.Increment(ref NextGlobalIndex);
            }
            finally {
                RegistrationFlag = 0;
            }

            return parameter;
        }

        #endregion
    }

    public sealed class ParameterFloat : Parameter {
        private readonly Func<IAutomatable, float> getter;
        private readonly Action<IAutomatable, float> setter;
        public new ParameterDescriptorFloat Descriptor => (ParameterDescriptorFloat) base.Descriptor;

        public ParameterFloat(Type ownerType, ParameterKey key, ParameterDescriptorFloat descriptor, Func<IAutomatable, float> getter, Action<IAutomatable, float> setter) : base(ownerType, key, descriptor) {
            this.getter = getter;
            this.setter = setter;
        }

        public override void SetValue(AutomationSequence sequence, long frame) {
            this.setter(sequence.AutomationData.Owner, sequence.GetFloatValue(frame));
        }

        public float GetEffectiveValue(IAutomatable automatable) => this.getter(automatable);
    }

    public sealed class ParameterDouble : Parameter {
        private readonly Func<IAutomatable, double> getter;
        private readonly Action<IAutomatable, double> setter;
        public new ParameterDescriptorDouble Descriptor => (ParameterDescriptorDouble) base.Descriptor;

        public ParameterDouble(Type ownerType, ParameterKey key, ParameterDescriptorDouble descriptor, Func<IAutomatable, double> getter, Action<IAutomatable, double> setter) : base(ownerType, key, descriptor) {
            this.getter = getter;
            this.setter = setter;
        }

        public override void SetValue(AutomationSequence sequence, long frame) {
            this.setter(sequence.AutomationData.Owner, sequence.GetDoubleValue(frame));
        }

        public double GetEffectiveValue(IAutomatable automatable) => this.getter(automatable);
    }

    public sealed class ParameterLong : Parameter {
        private readonly Func<IAutomatable, long> getter;
        private readonly Action<IAutomatable, long> setter;
        public new ParameterDescriptorLong Descriptor => (ParameterDescriptorLong) base.Descriptor;

        public ParameterLong(Type ownerType, ParameterKey key, ParameterDescriptorLong descriptor, Func<IAutomatable, long> getter, Action<IAutomatable, long> setter) : base(ownerType, key, descriptor) {
            this.getter = getter;
            this.setter = setter;
        }

        public override void SetValue(AutomationSequence sequence, long frame) {
            this.setter(sequence.AutomationData.Owner, sequence.GetLongValue(frame));
        }

        public long GetEffectiveValue(IAutomatable automatable) => this.getter(automatable);
    }

    public sealed class ParameterBoolean : Parameter {
        private readonly Func<IAutomatable, bool> getter;
        private readonly Action<IAutomatable, bool> setter;

        public new ParameterDescriptorBoolean Descriptor => (ParameterDescriptorBoolean) base.Descriptor;

        public ParameterBoolean(Type ownerType, ParameterKey key, ParameterDescriptorBoolean descriptor, Func<IAutomatable, bool> getter, Action<IAutomatable, bool> setter) : base(ownerType, key, descriptor) {
            this.getter = getter;
            this.setter = setter;
        }

        public override void SetValue(AutomationSequence sequence, long frame) {
            this.setter(sequence.AutomationData.Owner, sequence.GetBooleanValue(frame));
        }

        public bool GetEffectiveValue(IAutomatable automatable) => this.getter(automatable);
    }
}