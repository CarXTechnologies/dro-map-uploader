using System;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Serializable]
    public class IconType
    {
        public string name = "";
        public Sprite icon;
        public string sprite;
        public int size = 32;
        public int depth = 0;
        public Color color = Color.white;
    }

    [Serializable]
    public class TexturePair
    {
        public Texture mainTexture;
        public Texture alphaTexture;
    }

    [SerializeField] private LayerMask m_boundsLayers;
    [SerializeField] private Vector2 m_boundsCenter;
    [SerializeField] private Vector2 m_boundsSize;
    [SerializeField] private float m_baseZoom = 1f;
    [SerializeField] private TexturePair[] m_textures = null;
}