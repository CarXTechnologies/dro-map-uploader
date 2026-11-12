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
		[HideInInspector] public Texture alphaTexture;
	}

	public enum WorldMapPreviewMode
	{
		MinimapImage,
		Generated3D,
	}

	[Tooltip("World map preview of the map: the minimap image, or a 3D miniature generated from the map geometry at export")]
	[SerializeField] private WorldMapPreviewMode m_worldMapPreview = WorldMapPreviewMode.MinimapImage;
	[Tooltip("3D miniature exported with the map: Generate 3D preview writes it here, or set your own mesh in world space of the map " +
		"(submesh 0 roads and pads, 1 objects, 2 terrain). Empty: generated at export")]
	[SerializeField] private Mesh m_miniatureMesh;
	[SerializeField] private LayerMask m_boundsLayers;
	[SerializeField] private Vector2 m_boundsCenter;
	[SerializeField] private Vector2 m_boundsSize;
	[SerializeField] private TexturePair[] m_textures = null;
	[SerializeField] private bool m_lockSize = false;

	public TexturePair[] Textures => m_textures;
	public Vector2 BoundsCenter => m_boundsCenter;
	public Vector2 BoundsSize => m_boundsSize;
	public WorldMapPreviewMode WorldMapPreview => m_worldMapPreview;

	public Mesh MiniatureMesh
	{
		get => m_miniatureMesh;
		set => m_miniatureMesh = value;
	}

	private void OnValidate()
	{
		if (m_lockSize)
		{
			var lossyScale = transform.lossyScale;
			m_boundsSize.x = lossyScale.x;
			m_boundsSize.y = lossyScale.z;
		}
	}
}