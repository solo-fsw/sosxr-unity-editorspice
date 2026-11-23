using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;


namespace SOSXR.EditorSpice.Tests
{
    /// <summary>
    ///     Regression tests for the fixes from the audit of older EditorSpice code (2026-11-17).
    /// </summary>
    public class OlderCodeFixTests
    {
        private readonly List<Object> _created = new();


        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }


        // ── UnityEventDrawerWithButton: property path lookup ─────────────────

        [Serializable]
        private class Nested
        {
            public UnityEvent Direct = new();
            public List<UnityEvent> Events = new() {new UnityEvent(), new UnityEvent()};
        }


        [Serializable]
        private struct NestedStruct
        {
            public UnityEvent<int> Event;
        }


        private class Holder
        {
            public UnityEvent TopLevel = new();
            public Nested Inner = new();
            public NestedStruct Struct = new() {Event = new UnityEvent<int>()};
        }


        [Test]
        public void TryGetValueAtPath_TopLevelField()
        {
            var holder = new Holder();

            Assert.IsTrue(UnityEventButtonBase.TryGetValueAtPath(holder, "TopLevel", out var value));
            Assert.AreSame(holder.TopLevel, value);
        }


        [Test]
        public void TryGetValueAtPath_FieldInNestedClass()
        {
            var holder = new Holder();

            Assert.IsTrue(UnityEventButtonBase.TryGetValueAtPath(holder, "Inner.Direct", out var value));
            Assert.AreSame(holder.Inner.Direct, value);
        }


        [Test]
        public void TryGetValueAtPath_ListElement()
        {
            var holder = new Holder();

            Assert.IsTrue(UnityEventButtonBase.TryGetValueAtPath(holder, "Inner.Events.Array.data[1]", out var value));
            Assert.AreSame(holder.Inner.Events[1], value);
        }


        [Test]
        public void TryGetValueAtPath_FieldInStruct()
        {
            var holder = new Holder();

            Assert.IsTrue(UnityEventButtonBase.TryGetValueAtPath(holder, "Struct.Event", out var value));
            Assert.AreSame(holder.Struct.Event, value);
        }


        [TestCase("Missing")]
        [TestCase("Inner.Events.Array.data[5]")]
        [TestCase("Inner.Events.Array.data[-1]")]
        public void TryGetValueAtPath_InvalidPath_ReturnsFalse(string path)
        {
            Assert.IsFalse(UnityEventButtonBase.TryGetValueAtPath(new Holder(), path, out _));
        }


        // ── PackageIsInstalled ───────────────────────────────────────────────

        [Test]
        public void PackageIsInstalled_InstalledPackage_ReturnsTrue()
        {
            PackageIsInstalled.Cache.Clear();

            // The test framework is always installed when these tests run.
            Assert.IsTrue(PackageIsInstalled.PackageInstalled("com.unity.test-framework"));
        }


        [Test]
        public void PackageIsInstalled_UnknownPackage_ReturnsFalse()
        {
            PackageIsInstalled.Cache.Clear();

            Assert.IsFalse(PackageIsInstalled.PackageInstalled("com.sosxr.does-not-exist"));
            Assert.IsFalse(PackageIsInstalled.PackageInstalled(null));
        }


        // ── RightClickOptions: Extract Component to Child ────────────────────

        [Test]
        public void ExtractComponentToChild_ChildSitsOnParent()
        {
            var parent = new GameObject("ExtractParent");
            _created.Add(parent);
            parent.transform.localPosition = new Vector3(3f, 4f, 5f);
            parent.transform.localScale = new Vector3(2f, 2f, 2f);
            var collider = parent.AddComponent<BoxCollider>();

            RightClickOptions.ExtractMenuOption(new MenuCommand(collider));

            Assert.AreEqual(1, parent.transform.childCount);
            var child = parent.transform.GetChild(0);
            Assert.AreEqual(Vector3.zero, child.localPosition);
            Assert.AreEqual(Vector3.one, child.localScale);
            Assert.IsNotNull(child.GetComponent<BoxCollider>());
            Assert.IsNull(parent.GetComponent<BoxCollider>());
        }


        [Test]
        public void ExtractComponentToChild_Transform_DoesNothing()
        {
            var parent = new GameObject("ExtractTransform");
            _created.Add(parent);

            Assert.DoesNotThrow(() => RightClickOptions.ExtractMenuOption(new MenuCommand(parent.transform)));
            Assert.AreEqual(0, parent.transform.childCount);
        }
    }
}
