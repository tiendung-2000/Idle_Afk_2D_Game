using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelImmortal.Data
{
    /// <summary>
    /// Maps art ids to sprites. Gameplay code asks for an id and never references sprite assets directly,
    /// so replacing placeholder art with final art only means changing this catalog (or the sprites in it).
    /// A missing id is not an error: callers fall back to the built-in placeholder shapes.
    ///
    /// Id conventions: player, enemy.&lt;enemyId&gt;, boss.&lt;mapId&gt;, bg.&lt;mapId&gt;, vfx.&lt;skillId&gt;,
    /// ui.panel, ui.frame, ui.button, ui.button_pressed, ui.button_disabled, icon.&lt;resource&gt;.
    /// Multi-frame entries are animation frames in order.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "PixelImmortal/Art Catalog")]
    public class ArtCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public Sprite[] frames;
            /// <summary>Real coloured art: drawn untinted. Placeholder effects are near-white and get tinted by the skill's vfxColor.</summary>
            public bool fullColor;
        }

        public Entry[] entries;

        Dictionary<string, Sprite[]> lookup;
        HashSet<string> fullColorIds;

        void OnValidate() => lookup = null;

        void Build()
        {
            lookup = new Dictionary<string, Sprite[]>();
            fullColorIds = new HashSet<string>();
            if (entries == null) return;
            foreach (var e in entries)
                if (e != null && !string.IsNullOrEmpty(e.id) && e.frames != null && e.frames.Length > 0)
                {
                    lookup[e.id] = e.frames;
                    if (e.fullColor) fullColorIds.Add(e.id);
                }
        }

        /// <summary>True when the art for the id is real coloured art that must not be tinted.</summary>
        public bool IsFullColor(string id)
        {
            if (lookup == null) Build();
            return id != null && fullColorIds.Contains(id);
        }

        public bool Has(string id)
        {
            if (lookup == null) Build();
            return id != null && lookup.ContainsKey(id);
        }

        /// <summary>All frames for the id, or null when there is no art for it.</summary>
        public Sprite[] Frames(string id)
        {
            if (lookup == null) Build();
            return id != null && lookup.TryGetValue(id, out var f) ? f : null;
        }

        public Sprite First(string id)
        {
            var f = Frames(id);
            return f != null ? f[0] : null;
        }

        public IEnumerable<string> Ids
        {
            get
            {
                if (lookup == null) Build();
                return lookup.Keys;
            }
        }
    }
}
