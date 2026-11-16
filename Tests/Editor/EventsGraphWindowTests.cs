using EventVisualizer.Base;
using NUnit.Framework;
using UnityEditor;


namespace SOSXR.EditorSpice.Tests
{
    public class EventsGraphWindowTests
    {
        private const string PrefKey = "SOSXR.EditorSpice.Tests.SavedPrefBool";


        [TearDown]
        public void TearDown()
        {
            EditorPrefs.DeleteKey(PrefKey);
            EdgeTriggersTracker.Clear();
        }


        [Test]
        public void SavedPrefBool_Set_SameValue_DoesNotWriteEditorPrefs()
        {
            var pref = new EventsGraphWindow.SavedPrefBool(PrefKey, false);
            pref.Set(true);
            Assert.IsTrue(EditorPrefs.GetBool(PrefKey, false));

            EditorPrefs.DeleteKey(PrefKey);
            pref.Set(true);

            Assert.IsFalse(EditorPrefs.HasKey(PrefKey));
            Assert.IsTrue(pref.Get());
        }


        [Test]
        public void SavedPrefBool_Set_ChangedValue_WritesEditorPrefs()
        {
            var pref = new EventsGraphWindow.SavedPrefBool(PrefKey, true);
            pref.Set(false);

            Assert.IsTrue(EditorPrefs.HasKey(PrefKey));
            Assert.IsFalse(EditorPrefs.GetBool(PrefKey, true));
        }


        [Test]
        public void EdgeTriggersTracker_RegisterTrigger_CapsStoredTriggers()
        {
            EdgeTriggersTracker.Clear();

            for (var i = 0; i < EdgeTriggersTracker.MaxTriggers + 10; i++)
            {
                EdgeTriggersTracker.RegisterTrigger(null, null);
            }

            Assert.AreEqual(EdgeTriggersTracker.MaxTriggers, EdgeTriggersTracker.GetTimings((EventCall) null).Count);
        }


        [Test]
        public void EdgeTriggersTracker_Clear_RemovesAllData()
        {
            EdgeTriggersTracker.RegisterTrigger(null, null);
            EdgeTriggersTracker.Clear();

            Assert.IsFalse(EdgeTriggersTracker.HasData());
        }
    }
}
