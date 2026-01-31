using System.Drawing;
using System.Numerics;
using Rendering.UI;

namespace Managers
{
    /// <summary>
    /// <para>
    /// Exposes functions for managing (worldspace only) UIobjects like adding and removing 
    /// aswell as getting Objects in a given area, See <see cref="GetObjectsInVisibleArea(RectangleF)"/>
    /// </para>   
    /// </summary>
    
    public static class ChunkManager
    {
        private static readonly Dictionary<(int, int), List<UIObject>> _chunks = [];
        private static float _chunkSize;

        public static void Init(float chunkSize = 512f)
        {
            _chunkSize = chunkSize;
        }

        private static (int x, int y) GetChunkIndex(Vector2 worldPos)
        {
            return ((int)MathF.Floor(worldPos.X / _chunkSize), (int)MathF.Floor(worldPos.Y / _chunkSize));
        }

        public static void AddObject(UIObject obj)
        {
            // Add object to ALL chunks that its bounds overlap with
            var bounds = obj.Bounds;
            var minChunk = GetChunkIndex(new Vector2(bounds.Left, bounds.Top));
            var maxChunk = GetChunkIndex(new Vector2(bounds.Right, bounds.Bottom));

            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    if (!_chunks.TryGetValue((x, y), out var list))
                    {
                        list = [];
                        _chunks[(x, y)] = list;
                    }
                    list.Add(obj);
                }
            }
        }

        public static void RemoveObject(UIObject? obj)
        {
            if (obj == null) { return; }
            var keysToRemove = new List<(int, int)>();

            foreach (var kvp in _chunks)
            {
                if (kvp.Value.Remove(obj) && kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _chunks.Remove(key);
            }
        }

        public static void RemoveObjects<T>(IEnumerable<T> objects) where T : UIObject
        {
            foreach (var obj in objects)
            {
                RemoveObject(obj);
            }
        }

        public static void Clear()
        {
            foreach (var chunk in _chunks.Values)
            {
                foreach (var obj in chunk)
                {
                    obj.Dispose();
                }
            }

            _chunks.Clear();
        }

        public static IEnumerable<UIObject> GetObjectsInVisibleArea(RectangleF visibleArea)
        {
            var minX = (int)MathF.Floor(visibleArea.Left / _chunkSize);
            var maxX = (int)MathF.Floor(visibleArea.Right / _chunkSize);
            var minY = (int)MathF.Floor(visibleArea.Top / _chunkSize);
            var maxY = (int)MathF.Floor(visibleArea.Bottom / _chunkSize);

            HashSet<UIObject> returned = [];

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (_chunks.TryGetValue((x, y), out var chunkObjects))
                    {
                        foreach (var obj in chunkObjects)
                        {
                            if (obj.IntersectsWithRect(visibleArea))
                            {
                                if (returned.Add(obj))
                                    yield return obj;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<UIObject> GetAllObjects()
        {
            return _chunks.SelectMany(x => x.Value).Distinct();
        }
    }
}