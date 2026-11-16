using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;


namespace EventVisualizer.Base
{
    /// <summary>
    /// Records when edges fire their event calls, so the visualiser can draw a pulse along each edge.
    /// Triggers are kept in a static list and expire after TimeToLive seconds.
    /// </summary>
    public static class EdgeTriggersTracker
    {
        /// <summary>Time, in unscaled seconds, that a trigger stays visible before it expires.</summary>
        public static readonly float TimeToLive = 1f;

        /// <summary>Upper bound on stored triggers. Oldest are dropped first, so a burst of events cannot grow the list without limit.</summary>
        public const int MaxTriggers = 1000;
        private static readonly List<EdgeTrigger> triggers = new();


        /// <summary>
        /// Records that the event call fired on the edge at the current unscaled time.
        /// If more than MaxTriggers are stored, the oldest are removed.
        /// </summary>
        /// <param name="edge">The edge the event call travels along.</param>
        /// <param name="eventCall">The event call that fired.</param>
        public static void RegisterTrigger(Edge edge, EventCall eventCall)
        {
            triggers.Add(new EdgeTrigger {edge = edge, eventCall = eventCall, triggeredTime = Time.unscaledTime});

            if (triggers.Count > MaxTriggers)
            {
                triggers.RemoveRange(0, triggers.Count - MaxTriggers);
            }
        }


        /// <summary>
        /// Returns the normalized age of each live trigger for the event call, from 0 (just fired) to 1 (about to expire).
        /// Expired triggers are removed from the list.
        /// </summary>
        /// <param name="eventCall">The event call to look up.</param>
        /// <returns>The normalized age of each trigger that has not expired.</returns>
        public static List<float> GetTimings(EventCall eventCall)
        {
            var now = Time.unscaledTime;
            var acceptedTriggers = triggers.FindAll(t => t.eventCall == eventCall);

            return GetTimings(acceptedTriggers);
        }


        /// <summary>
        /// Returns the normalized age of each live trigger on the edge, from 0 (just fired) to 1 (about to expire).
        /// Expired triggers are removed from the list.
        /// </summary>
        /// <param name="edge">The edge to look up.</param>
        /// <returns>The normalized age of each trigger that has not expired.</returns>
        public static List<float> GetTimings(Edge edge)
        {
            var acceptedTriggers = triggers.FindAll(t => t.edge == edge);

            return GetTimings(acceptedTriggers);
        }


        private static List<float> GetTimings(List<EdgeTrigger> acceptedTriggers)
        {
            var now = Time.unscaledTime;
            var timings = new List<float>(); //TODO cache

            foreach (var t in acceptedTriggers)
            {
                var time = Mathf.Abs(t.triggeredTime - now) / TimeToLive;

                if (time <= 1f)
                {
                    timings.Add(time);
                }
                else
                {
                    triggers.Remove(t);
                }
            }

            return timings;
        }


        /// <summary>Removes all triggers older than TimeToLive.</summary>
        public static void CleanObsolete()
        {
            var now = Time.unscaledTime;
            triggers.RemoveAll(trigger => Mathf.Abs(now - trigger.triggeredTime) > TimeToLive);
        }


        /// <summary>Removes all recorded triggers.</summary>
        public static void Clear()
        {
            triggers.Clear();
        }


        /// <summary>
        /// Checks whether any triggers are stored. Expired triggers still count until CleanObsolete or GetTimings removes them.
        /// </summary>
        /// <returns>True if the trigger list is not empty.</returns>
        public static bool HasData()
        {
            return triggers.Count > 0;
        }


        /// <summary>One recorded firing of an event call on an edge.</summary>
        public class EdgeTrigger
        {
            /// <summary>The event call that fired.</summary>
            public EventCall eventCall;
            /// <summary>The edge the event call travels along.</summary>
            public Edge edge;
            /// <summary>Time.unscaledTime at the moment the event fired.</summary>
            public float triggeredTime;
        }
    }
}