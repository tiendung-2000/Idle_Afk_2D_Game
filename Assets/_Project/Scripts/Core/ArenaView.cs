using PixelImmortal.Data;
using UnityEngine;

namespace PixelImmortal.Core
{
    /// <summary>Fixed arena: placeholder background/border and spawn-point helper.</summary>
    public class ArenaView : MonoBehaviour
    {
        const float Border = 0.25f;
        const float SpawnInset = 0.4f;

        SpriteRenderer background, frame;
        Rect bounds;
        ArtCatalog art;

        public Rect Bounds => bounds;

        public void Init(Vector2 size, ArtCatalog artCatalog = null)
        {
            art = artCatalog;
            bounds = new Rect(-size * 0.5f, size);

            frame = new GameObject("Frame").AddComponent<SpriteRenderer>();
            frame.transform.SetParent(transform, false);
            frame.sprite = PlaceholderSprites.Square;
            frame.sortingOrder = -101;
            frame.transform.localScale = new Vector3(size.x + Border * 2f, size.y + Border * 2f, 1f);

            background = new GameObject("Background").AddComponent<SpriteRenderer>();
            background.transform.SetParent(transform, false);
            background.sprite = PlaceholderSprites.Square;
            background.sortingOrder = -100;
            background.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public void ApplyMap(MapDefinition map)
        {
            frame.color = map.borderColor;
            var bg = art != null ? art.First("bg." + map.mapId) : null;
            if (bg != null)
            {
                // Art background: stretch the sprite over the arena rectangle, no tint.
                background.sprite = bg;
                background.color = Color.white;
                var s = bg.bounds.size;
                background.transform.localScale = new Vector3(bounds.width / s.x, bounds.height / s.y, 1f);
            }
            else
            {
                background.sprite = PlaceholderSprites.Square;
                background.color = map.backgroundColor;
                background.transform.localScale = new Vector3(bounds.width, bounds.height, 1f);
            }
        }

        public Vector2 RandomEdgePoint(Vector2 avoid, float minDistance)
        {
            Vector2 p = Vector2.zero;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float x = Random.Range(bounds.xMin + SpawnInset, bounds.xMax - SpawnInset);
                float y = Random.Range(bounds.yMin + SpawnInset, bounds.yMax - SpawnInset);
                switch (Random.Range(0, 4))
                {
                    case 0: p = new Vector2(x, bounds.yMax - SpawnInset); break;
                    case 1: p = new Vector2(x, bounds.yMin + SpawnInset); break;
                    case 2: p = new Vector2(bounds.xMin + SpawnInset, y); break;
                    default: p = new Vector2(bounds.xMax - SpawnInset, y); break;
                }
                if ((p - avoid).sqrMagnitude >= minDistance * minDistance) break;
            }
            return p;
        }
    }
}
