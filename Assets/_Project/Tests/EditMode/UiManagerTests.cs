using NUnit.Framework;
using PixelImmortal.UI;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class UiManagerTests
    {
        class TestScreen : UIScreen
        {
            public int Enters, Refreshes, Exits;
            public object LastArgs;
            public override void OnEnter(object args) { Enters++; LastArgs = args; }
            public override void OnRefresh() => Refreshes++;
            public override void OnExit() => Exits++;
        }

        UIManager ui;
        int created;

        [SetUp]
        public void SetUp()
        {
            ui = UIManager.Create();
            created = 0;
            ui.Navigator.Register("a", p => Make("a", p));
            ui.Navigator.Register("b", p => Make("b", p));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(ui.gameObject);

        UIScreen Make(string name, RectTransform parent)
        {
            created++;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TestScreen>();
        }

        [Test]
        public void PushPopKeepsLifecycleAndStack()
        {
            Assert.IsTrue(ui.Navigator.Push("a", "hello"));
            var a = (TestScreen)ui.Navigator.Current;
            Assert.AreEqual("hello", a.LastArgs);
            ui.Navigator.Push("b");
            Assert.IsFalse(a.gameObject.activeSelf, "covered screen is hidden");
            ui.Navigator.Pop();
            Assert.IsTrue(a.gameObject.activeSelf);
            Assert.AreEqual(1, a.Refreshes);
            Assert.AreEqual(0, a.Exits);
            ui.Navigator.Pop();
            Assert.AreEqual(1, a.Exits);
            Assert.AreEqual(0, ui.Navigator.Depth);
        }

        [Test]
        public void ReopenedScreenIsReusedAndDuplicatePushIgnored()
        {
            ui.Navigator.Push("a");
            Assert.IsFalse(ui.Navigator.Push("a"), "already open");
            ui.Navigator.Pop();
            ui.Navigator.Push("a");
            Assert.AreEqual(1, created, "one instance for the whole session");
        }

        [Test]
        public void ReplaceSwapsTopWithoutRevealingBelow()
        {
            ui.Navigator.Push("a");
            var a = (TestScreen)ui.Navigator.Current;
            Assert.IsTrue(ui.Navigator.Replace("b"));
            Assert.AreEqual(1, ui.Navigator.Depth);
            Assert.AreEqual("b", ui.Navigator.Current.Id);
            Assert.AreEqual(1, a.Exits);
        }

        [Test]
        public void UnknownScreenIsRejected()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not registered"));
            Assert.IsFalse(ui.Navigator.Push("nope"));
        }

        [Test]
        public void BackClosesPopupThenHandlerThenScreenThenRoot()
        {
            int root = 0, handler = 0;
            bool handlerOpen = true;
            ui.BackAtRoot = () => root++;
            ui.AddBackHandler(() => { if (!handlerOpen) return false; handlerOpen = false; handler++; return true; });
            ui.Navigator.Push("a");

            int cancelled = 0;
            ui.Confirm.Show("msg", "ok", null, "cancel", () => cancelled++, true);

            ui.HandleBack();
            Assert.IsFalse(ui.Confirm.IsOpen, "popup closes first");
            Assert.AreEqual(1, cancelled);
            Assert.AreEqual(1, ui.Navigator.Depth);

            ui.HandleBack();
            Assert.AreEqual(1, handler);
            Assert.AreEqual(1, ui.Navigator.Depth);

            ui.HandleBack();
            Assert.AreEqual(0, ui.Navigator.Depth);

            ui.HandleBack();
            Assert.AreEqual(1, root);
        }

        [Test]
        public void CriticalPopupSwallowsBack()
        {
            ui.Navigator.Push("a");
            ui.Confirm.Show("msg", "ok", null);
            Assert.IsTrue(ui.HandleBack());
            Assert.IsTrue(ui.Confirm.IsOpen, "one-button popups are critical");
            Assert.AreEqual(1, ui.Navigator.Depth, "Back must not reach the screen below");
        }

        [Test]
        public void ToastShowsAndLayersAreOrdered()
        {
            ui.ShowToast("hi", ToastSeverity.Warning);
            Assert.IsTrue(ui.ToastLayer.GetChild(0).gameObject.activeSelf);
            Assert.Less(ui.HudLayer.GetSiblingIndex(), ui.PopupLayer.GetSiblingIndex());
            Assert.Less(ui.PopupLayer.GetSiblingIndex(), ui.ToastLayer.GetSiblingIndex());
            Assert.Less(ui.ScreenLayer.GetSiblingIndex(), ui.HudLayer.GetSiblingIndex());
        }
    }
}
