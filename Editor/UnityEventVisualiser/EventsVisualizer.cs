using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventVisualizer.Puppy;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;


namespace EventVisualizer.Base
{
    public static class EventsFinder
    {
        public static bool NeedsGraphRefresh = false;

        private static readonly HashSet<Type> ComponentsThatCanHaveUnityEvent = new();
        private static readonly Dictionary<Type, bool> TmpSearchedTypes = new();


        public static List<EventCall> FindAllEvents(GameObject[] roots, bool searchHierarchy = true)
        {
            var calls = new HashSet<EventCall>();

            foreach (var type in ComponentsThatCanHaveUnityEvent)
            {
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                var selectedComponents = new HashSet<Object>();

                if (roots != null && roots.Length > 0)
                {
                    foreach (var root in roots)
                    {
                        if (root != null)
                        {
                            if (searchHierarchy)
                            {
                                selectedComponents.UnionWith(root.GetComponentsInChildren(type));
                            }
                            else
                            {
                                selectedComponents.Add(root.GetComponent(type));
                            }
                        }
                    }
                }
                else
                {
                    selectedComponents = new HashSet<Object>(GameObject.FindObjectsByType(type, FindObjectsInactive.Exclude));
                }

                foreach (var caller in selectedComponents)
                {
                    var comp = caller as Component;

                    if (comp != null)
                    {
                        ExtractDefaultEventTriggers(calls, comp);
                        ExtractEvents(calls, comp);
                    }
                }
            }

            return calls.ToList();
        }


        private static void ExtractEvents(HashSet<EventCall> calls, Component caller)
        {
            var iterator = new SerializedObject(caller).GetIterator();
            iterator.Next(true);
            RecursivelyExtractEvents(calls, caller, iterator, 0);
        }


        private static bool RecursivelyExtractEvents(HashSet<EventCall> calls, Component caller, SerializedProperty iterator, int level)
        {
            var hasData = true;

            do
            {
                var persistentCalls = iterator.FindPropertyRelative("m_PersistentCalls.m_Calls");
                var isUnityEvent = persistentCalls != null;

                if (isUnityEvent && persistentCalls.arraySize > 0)
                {
                    var unityEvent = EditorHelper.GetTargetObjectOfProperty(iterator) as UnityEventBase;
                    AddEventCalls(calls, caller, unityEvent, iterator.displayName, iterator.propertyPath);
                }

                hasData = iterator.Next(!isUnityEvent);

                if (hasData)
                {
                    if (iterator.depth < level)
                    {
                        return hasData;
                    }

                    if (iterator.depth > level)
                    {
                        hasData = RecursivelyExtractEvents(calls, caller, iterator, iterator.depth);
                    }
                }
            } while (hasData);

            return false;
        }


        private static void ExtractDefaultEventTriggers(HashSet<EventCall> calls, Component caller)
        {
            var eventTrigger = caller as EventTrigger;

            if (eventTrigger != null)
            {
                foreach (var trigger in eventTrigger.triggers)
                {
                    var name = trigger.eventID.ToString();
                    AddEventCalls(calls, caller, trigger.callback, name, name);
                }
            }
        }


        private static void AddEventCalls(HashSet<EventCall> calls, Component caller, UnityEventBase unityEvent, string eventShortName, string eventFullName)
        {
            for (var i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                var methodName = unityEvent.GetPersistentMethodName(i);
                var receiver = unityEvent.GetPersistentTarget(i);

                if (receiver != null && methodName != null && methodName != "")
                {
                    calls.Add(new EventCall(caller, receiver, eventShortName, eventFullName, methodName, unityEvent));
                }
            }
        }


        // InitializeOnLoadMethod runs after every domain reload. [DidReloadScripts] as well made the scan run twice.
        [InitializeOnLoadMethod]
        private static void RefreshTypesThatCanHoldUnityEvents()
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<Type> objects;

            try
            {
                objects = TypeCache.GetTypesDerivedFrom<Component>();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Assembly operation failed: {e.Message}");
                return;
            }

            foreach (var obj in objects)
            {
                if (RecursivelySearchFields<UnityEventBase>(obj))
                {
                    ComponentsThatCanHaveUnityEvent.Add(obj);
                }
            }

            TmpSearchedTypes.Clear();

            // Debug.Log("UnityEventVisualizer Updated Components that can have UnityEvents (" + ComponentsThatCanHaveUnityEvent.Count + "). Milliseconds: " + sw.Elapsed.TotalMilliseconds);
        }


        /// <summary>
        ///     Search for types that have a field or property of type <typeparamref name="T" /> or can hold an object that can.
        /// </summary>
        /// <typeparam name="T">Needle</typeparam>
        /// <param name="type">Haystack</param>
        /// <returns>Can contain some object <typeparamref name="T" /></returns>
        private static bool RecursivelySearchFields<T>(Type type)
        {
            bool wanted;

            if (TmpSearchedTypes.TryGetValue(type, out wanted))
            {
                return wanted;
            }

            TmpSearchedTypes.Add(type, false);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var fType in type.GetFields(flags).Where(f => !f.FieldType.IsPrimitive).Select(f => f.FieldType).Concat(type.GetProperties(flags).Select(p => p.PropertyType)))
            {
                if (typeof(T).IsAssignableFrom(fType))
                {
                    return TmpSearchedTypes[type] |= true;
                }

                if (typeof(Object).IsAssignableFrom(fType))
                {
                }
                else if (!TmpSearchedTypes.TryGetValue(fType, out wanted))
                {
                    if (RecursivelySearchFields<T>(fType))
                    {
                        return TmpSearchedTypes[type] |= true;
                    }
                }
                else if (wanted)
                {
                    return TmpSearchedTypes[type] |= true;
                }
            }

            if (type.IsArray)
            {
                if (RecursivelySearchFields<T>(type.GetElementType()))
                {
                    return TmpSearchedTypes[type] |= true;
                }
            }

            return false;
        }
    }
}
