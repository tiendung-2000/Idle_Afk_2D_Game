using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Screen stack: Push opens a feature screen over the previous one, Pop returns, Replace swaps the top.
    /// Screens are created lazily from a registered factory and reused, so there is never a duplicate instance.
    /// </summary>
    public sealed class ScreenNavigator
    {
        readonly RectTransform layer;
        readonly Dictionary<string, Func<RectTransform, UIScreen>> factories = new Dictionary<string, Func<RectTransform, UIScreen>>();
        readonly Dictionary<string, UIScreen> instances = new Dictionary<string, UIScreen>();
        readonly List<UIScreen> stack = new List<UIScreen>();

        public ScreenNavigator(RectTransform screenLayer) => layer = screenLayer;

        public int Depth => stack.Count;
        public UIScreen Current => stack.Count > 0 ? stack[stack.Count - 1] : null;

        /// <summary>Raised after every push, pop or replace.</summary>
        public event Action Changed;

        /// <param name="factory">Builds the screen under the given parent (instantiate a prefab or add a component).</param>
        public void Register(string id, Func<RectTransform, UIScreen> factory)
        {
            if (string.IsNullOrEmpty(id) || factory == null) throw new ArgumentException("A screen needs an id and a factory.");
            factories[id] = factory;
        }

        public bool Contains(string id) => stack.Exists(s => s.Id == id);

        public bool Push(string id, object args = null)
        {
            if (!CanOpen(id)) return false;
            var screen = GetOrCreate(id);
            if (screen == null) return false;

            Current?.Hide();
            stack.Add(screen);
            screen.Show();
            screen.OnEnter(args);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Closes the top screen and reveals the one below. False when no screen is open.</summary>
        public bool Pop()
        {
            if (stack.Count == 0) return false;
            RemoveTop();
            Current?.Show();
            Current?.OnRefresh();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Swaps the top screen for another without exposing the screen below.</summary>
        public bool Replace(string id, object args = null)
        {
            if (!CanOpen(id)) return false;
            var screen = GetOrCreate(id);
            if (screen == null) return false;

            if (stack.Count > 0) RemoveTop();
            stack.Add(screen);
            screen.Show();
            screen.OnEnter(args);
            Changed?.Invoke();
            return true;
        }

        void RemoveTop()
        {
            var top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            top.Hide();
            top.OnExit();
        }

        bool CanOpen(string id)
        {
            if (!factories.ContainsKey(id)) { Debug.LogWarning("UI: screen not registered: " + id); return false; }
            if (Contains(id)) return false; // already open: ignore repeated taps
            return true;
        }

        UIScreen GetOrCreate(string id)
        {
            if (instances.TryGetValue(id, out var existing) && existing != null) return existing;
            var screen = factories[id](layer);
            if (screen == null) { Debug.LogError("UI: factory returned no screen for " + id); return null; }
            screen.Id = id;
            screen.gameObject.SetActive(false);
            instances[id] = screen;
            return screen;
        }
    }
}
