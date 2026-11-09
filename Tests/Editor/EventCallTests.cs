using EventVisualizer.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;


namespace SOSXR.EditorSpice.Tests
{
    public class EventCallTests
    {
        private GameObject _sender;
        private GameObject _receiver;
        private ScriptableObject _scriptableObject;


        [SetUp]
        public void SetUp()
        {
            _sender = new GameObject("Sender");
            _receiver = new GameObject("Receiver");
            _scriptableObject = ScriptableObject.CreateInstance<Tarodev.AutoSaveConfig>();
        }


        [TearDown]
        public void TearDown()
        {
            NodeData.ClearAll();
            Object.DestroyImmediate(_sender);
            Object.DestroyImmediate(_receiver);
            Object.DestroyImmediate(_scriptableObject);
        }


        [Test]
        public void Constructor_AcceptsGameObjectAndScriptableObject()
        {
            // Regression: (Component) cast threw InvalidCastException for non-Component targets
            EventCall call = null;

            Assert.DoesNotThrow(() => call = new EventCall(_sender, _scriptableObject, "e", "e", "M", null));
            Assert.AreSame(_sender, call.sender);
            Assert.AreSame(_scriptableObject, call.receiver);
        }


        [Test]
        public void Constructor_MapsComponentToItsGameObject()
        {
            var call = new EventCall(_sender.transform, _receiver.transform, "e", "e", "M", null);

            Assert.AreSame(_sender, call.sender);
            Assert.AreSame(_receiver, call.receiver);
        }


        [Test]
        public void Constructor_DoesNotAttachListener()
        {
            var unityEvent = new UnityEvent();
            var call = new EventCall(_sender, _receiver, "e", "e", "M", unityEvent);

            Assert.IsFalse(call.IsAttached);
            unityEvent.Invoke();
            Assert.AreEqual(0, call.timesExecuted);
        }


        [Test]
        public void Attach_IsIdempotent()
        {
            var unityEvent = new UnityEvent();
            var call = new EventCall(_sender, _receiver, "e", "e", "M", unityEvent);

            call.Attach();
            call.Attach();
            unityEvent.Invoke();

            Assert.AreEqual(1, call.timesExecuted);
            call.Detach();
        }


        [Test]
        public void Detach_RemovesListener()
        {
            var unityEvent = new UnityEvent<int>();
            var call = new EventCall(_sender, _receiver, "e", "e", "M", unityEvent);
            var triggered = 0;

            call.Attach();
            call.OnTriggered = () => triggered++;
            unityEvent.Invoke(1);
            call.Detach();
            unityEvent.Invoke(2);

            Assert.AreEqual(1, call.timesExecuted);
            Assert.AreEqual(1, triggered);
            Assert.IsFalse(call.IsAttached);
            Assert.IsNull(call.OnTriggered);
        }


        [Test]
        public void Attach_SupportsFourArgumentEvents()
        {
            // Regression: the four-argument branch checked Length == 2 and never ran
            var unityEvent = new UnityEvent<int, int, int, int>();
            var call = new EventCall(_sender, _receiver, "e", "e", "M", unityEvent);

            call.Attach();
            unityEvent.Invoke(1, 2, 3, 4);

            Assert.AreEqual(1, call.timesExecuted);
            call.Detach();
        }


        [Test]
        public void NodeData_ClearAll_DetachesRegisteredCalls()
        {
            var unityEvent = new UnityEvent();
            var call = new EventCall(_sender, _receiver, "e", "e", "M", unityEvent);

            NodeData.RegisterEvent(call);
            Assert.IsTrue(call.IsAttached);

            NodeData.ClearAll();
            Assert.IsFalse(call.IsAttached);

            unityEvent.Invoke();
            Assert.AreEqual(0, call.timesExecuted);
        }
    }
}
