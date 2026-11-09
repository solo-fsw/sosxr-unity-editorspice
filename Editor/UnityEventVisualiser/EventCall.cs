using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;


namespace EventVisualizer.Base
{
    [Serializable]
    public class EventCall
    {
        public NodeData nodeSender;
        public NodeData nodeReceiver;
        public readonly Object sender;
        public readonly Object receiver;
        public readonly string eventShortName;
        public readonly string eventFullName;
        public readonly string method;
        public readonly Color color;
        public readonly UnityEventBase unityEvent;

        public Action OnTriggered;

        private static Regex parenteshesPattern = new(@"\(([^\(]*)\)$");


        public EventCall(Object sender, Object receiver, string eventShortName, string eventFullName, string methodName, UnityEventBase unityEvent)
        {
            // Senders and receivers can be GameObjects or ScriptableObjects too. Only Components map to their GameObject.
            this.sender = sender is Component senderComponent && senderComponent ? senderComponent.gameObject : sender;
            this.receiver = receiver is Component receiverComponent && receiverComponent ? receiverComponent.gameObject : receiver;
            this.eventShortName = eventShortName;
            this.eventFullName = eventFullName;
            method = methodName;
            color = EdgeGUI.ColorForIndex(this.eventShortName);
            this.unityEvent = unityEvent;

            UpdateReceiverComponentName(receiver);

            // The listener is attached in NodeData.RegisterEvent, so duplicate calls that are discarded never leak a listener.
        }


        public string ReceiverComponentName { get; private set; }
        public string ReceiverComponentNameSimple { get; private set; }
        public double lastTimeExecuted { get; private set; }
        public int timesExecuted { get; private set; }

        public string MethodFullPath => ReceiverComponentName + "." + method;


        private static readonly string[] TriggerMethodNames =
        {
            nameof(TriggerZeroArgs),
            nameof(TriggerOneArg),
            nameof(TriggerTwoArgs),
            nameof(TriggerThreeArgs),
            nameof(TriggerFourArgs)
        };

        private static readonly Type[] UnityActionTypes =
        {
            typeof(UnityAction),
            typeof(UnityAction<>),
            typeof(UnityAction<,>),
            typeof(UnityAction<,,>),
            typeof(UnityAction<,,,>)
        };

        private Delegate _triggerAction;

        /// <summary>True while this call has a listener registered on its UnityEvent.</summary>
        public bool IsAttached => _triggerAction != null;


        /// <summary>
        ///     Adds a listener to the UnityEvent so the visualiser can show when it fires.
        ///     Idempotent: calling it twice registers only one listener.
        /// </summary>
        public void Attach()
        {
            if (unityEvent == null || _triggerAction != null)
            {
                return;
            }

            var eventRegisterMethod = unityEvent.GetType().GetMethod("AddListener");

            if (eventRegisterMethod == null)
            {
                return;
            }

            var eventType = eventRegisterMethod.GetParameters()[0].ParameterType;
            var eventParameters = eventType.GetMethod("Invoke").GetParameters();
            var argCount = eventParameters.Length;

            if (argCount >= TriggerMethodNames.Length)
            {
                return;
            }

            var argTypes = eventParameters.Select(p => p.ParameterType).ToArray();
            var methodInfo = GetType().GetMethod(TriggerMethodNames[argCount], BindingFlags.Public | BindingFlags.Instance);
            var actionT = UnityActionTypes[argCount];

            if (argCount > 0)
            {
                methodInfo = methodInfo.MakeGenericMethod(argTypes);
                actionT = actionT.MakeGenericType(argTypes);
            }

            var triggerAction = Delegate.CreateDelegate(actionT, this, methodInfo);

            eventRegisterMethod.Invoke(unityEvent, new object[]
            {
                triggerAction
            });

            _triggerAction = triggerAction;
        }


        /// <summary>
        ///     Removes the listener added by <see cref="Attach" /> and clears <see cref="OnTriggered" />.
        ///     Call before discarding the EventCall, otherwise the UnityEvent keeps it alive.
        /// </summary>
        public void Detach()
        {
            OnTriggered = null;

            if (_triggerAction == null)
            {
                return;
            }

            var eventRemoveMethod = unityEvent?.GetType().GetMethod("RemoveListener");

            eventRemoveMethod?.Invoke(unityEvent, new object[]
            {
                _triggerAction
            });

            _triggerAction = null;
        }


        private void UpdateReceiverComponentName(Object component)
        {
            if (receiver != null)
            {
                var matches = parenteshesPattern.Matches(component.ToString());

                if (matches != null && matches.Count == 1)
                {
                    ReceiverComponentName = matches[0].Value;
                    ReceiverComponentName = ReceiverComponentName.Substring(1, ReceiverComponentName.Length - 2);
                    var lastDot = ReceiverComponentName.LastIndexOf('.') + 1;
                    ReceiverComponentNameSimple = ReceiverComponentName.Substring(lastDot, ReceiverComponentName.Length - lastDot);
                }
            }
        }


        public override bool Equals(object obj)
        {
            var ec = (EventCall) obj;

            return null != ec && ec.unityEvent == unityEvent && receiver == ec.receiver && method == ec.method;
        }


        public override int GetHashCode()
        {
            return unityEvent == null ? 0 : unityEvent.GetHashCode() ^ (receiver == null ? 0 : receiver.GetHashCode() ^ method.GetHashCode());
        }


        #region generic callers

        public void TriggerZeroArgs()
        {
            OnExecuted();

            OnTriggered?.Invoke();
        }


        public void TriggerOneArg<T0>(T0 arg0)
        {
            OnExecuted();

            OnTriggered?.Invoke();
        }


        public void TriggerTwoArgs<T0, T1>(T0 arg0, T1 arg1)
        {
            OnExecuted();

            OnTriggered?.Invoke();
        }


        public void TriggerThreeArgs<T0, T1, T2>(T0 arg, T1 arg1, T2 arg2)
        {
            OnExecuted();

            OnTriggered?.Invoke();
        }


        public void TriggerFourArgs<T0, T1, T2, T3>(T0 arg, T1 arg1, T2 arg2, T3 arg3)
        {
            OnExecuted();

            OnTriggered?.Invoke();
        }


        private void OnExecuted()
        {
            timesExecuted++;
            lastTimeExecuted = EditorApplication.timeSinceStartup;
        }

        #endregion
    }
}