using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using Expression = System.Linq.Expressions.Expression;

namespace FramePFX.Editors.Controls {
    internal static class BinderUtils {
        internal static readonly MethodInfo InvokeActionMethod = typeof(Action).GetMethod("Invoke");
        internal static readonly Dictionary<Type, CachedEventTypeInfo> CachedEventTypeMap = new Dictionary<Type, CachedEventTypeInfo>();

        internal class CachedEventTypeInfo {
            public ParameterExpression[] paramExpressions;
        }
    }

    /// <summary>
    /// A class that helps bind between a model and view, via a custom event
    /// </summary>
    public class Binder<TModel> where TModel : class {
        public DependencyProperty Property { get; }

        public FrameworkElement Element { get; }

        public bool IsAttached { get; private set; }

        public TModel Model { get; private set; }

        private readonly EventInfo eventInfo;
        private Func<Binder<TModel>, object> getModelValue;
        private Action<Binder<TModel>, object> setModelValue;
        private Action<Binder<TModel>> updateView;
        private Action<Binder<TModel>> updateModel;

        private bool isUpdatingView;
        private Delegate HandlerInternal;

        private Binder(FrameworkElement element, DependencyProperty property, string eventName) {
            this.Element = element;
            this.Property = property;
            EventInfo info = typeof(TModel).GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            if (info == null) {
                throw new Exception("Could not find event by name: " + typeof(TModel).Name + "." + eventName);
            }

            this.eventInfo = info;
        }

        /// <summary>
        /// Automatically sets the dependency property value, and uses the given function and action to get/set the model value
        /// </summary>
        public static Binder<TModel> AutoSet<TValue>(FrameworkElement element,
                                                     DependencyProperty property,
                                                     string eventName,
                                                     Func<TModel, TValue> getter,
                                                     Action<TModel, TValue> setter) {
            Binder<TModel> binder = NewInstance(element, property, eventName);
            binder.getModelValue = (a) => getter(a.Model);
            binder.setModelValue = (b, val) => setter(b.Model, (TValue) val);
            return binder;
        }

        /// <summary>
        /// Similar to <see cref="AutoSet{TValue}"/> except you provide the actions that set the model and set the view values
        /// </summary>
        public static Binder<TModel> Updaters(FrameworkElement element,
                                              DependencyProperty property,
                                              string eventName,
                                              Action<Binder<TModel>> updateView,
                                              Action<Binder<TModel>> updateModel) {
            Binder<TModel> binder = NewInstance(element, property, eventName);
            binder.updateView = updateView;
            binder.updateModel = updateModel;
            return binder;
        }

        private static Binder<TModel> NewInstance(FrameworkElement element,
                                                  DependencyProperty property,
                                                  string eventName) {
            Binder<TModel> binder = new Binder<TModel>(element, property, eventName);
            binder.HandlerInternal = CreateDelegateToInvokeActionFromEventHandler(eventName, binder.OnEvent);
            return binder;
        }

        private void OnEvent() {
            if (this.isUpdatingView)
                return;
            this.UpdateViewValue();
        }

        private void UpdateViewValue() {
            try {
                this.isUpdatingView = true;
                if (this.updateView != null) {
                    this.updateView(this);
                }
                else if (this.getModelValue != null) {
                    object value = this.getModelValue(this);
                    this.Element.SetValue(this.Property, value);
                }
            }
            finally {
                this.isUpdatingView = false;
            }
        }

        private static Delegate CreateDelegateToInvokeActionFromEventHandler(string eventName, Action actionToInvoke) {
            Type modelType = typeof(TModel);
            EventInfo eventInfo = modelType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            if (eventInfo == null) {
                throw new ArgumentException($"Event '{eventName}' not found on type '{modelType.FullName}'.");
            }

            Type eventType = eventInfo.EventHandlerType;
            if (!BinderUtils.CachedEventTypeMap.TryGetValue(eventType, out BinderUtils.CachedEventTypeInfo info)) {
                BinderUtils.CachedEventTypeMap[eventType] = info = new BinderUtils.CachedEventTypeInfo();
                MethodInfo invokeMethod = eventType.GetMethod("Invoke") ?? throw new Exception(eventType.Name + " is not a delegate type");
                info.paramExpressions = invokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
            }

            // This can't really be optimised any further
            MethodCallExpression invokeAction = Expression.Call(Expression.Constant(actionToInvoke), BinderUtils.InvokeActionMethod);
            return Expression.Lambda(eventType, invokeAction, info.paramExpressions).Compile();
        }

        public void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            if (!this.isUpdatingView && e.Property == this.Property && this.Property != null) {
                if (this.updateModel != null) {
                    this.updateModel(this);
                }
                else if (this.setModelValue != null) {
                    this.setModelValue(this, e.NewValue);
                }
            }
        }

        public void Attach(TModel model) => this.Attach(true, model);

        public void Attach(bool autoUpdateElementValue, TModel model) {
            if (this.IsAttached)
                throw new Exception("Already attached");
            this.Model = model ?? throw new Exception("Cannot use null model");
            this.eventInfo.AddEventHandler(this.Model, this.HandlerInternal);
            this.IsAttached = true;
            if (autoUpdateElementValue)
                this.UpdateViewValue();
        }

        public void Detatch() {
            if (!this.IsAttached)
                throw new Exception("Not attached");
            this.eventInfo.RemoveEventHandler(this.Model, this.HandlerInternal);
            this.Model = null;
            this.IsAttached = false;
        }
    }
}