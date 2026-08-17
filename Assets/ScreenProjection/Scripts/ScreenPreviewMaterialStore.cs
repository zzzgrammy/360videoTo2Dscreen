using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turn360To2D
{
    /// <summary>Owns one preview material per output screen so RenderTextures never overwrite each other.</summary>
    public sealed class ScreenPreviewMaterialStore : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public ScreenDirection direction;
            public Material material;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public Material Get(ScreenDirection direction)
        {
            foreach (Entry entry in entries)
                if (entry.direction == direction) return entry.material;
            return null;
        }

        public void Set(ScreenDirection direction, Material material)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].direction != direction) continue;
                entries[index] = new Entry { direction = direction, material = material };
                return;
            }
            entries.Add(new Entry { direction = direction, material = material });
        }
    }
}
