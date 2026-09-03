using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CaptureCamera : MonoBehaviour
{
	[SerializeField] private int m_width = 1024;
	[SerializeField] private int m_height = 1024;

	[SerializeField] private int m_samples = 1;

	[SerializeField] private bool m_clipBackground;

	public int Width => m_width;
	public int Height => m_height;
	public int Samples => m_samples;
	public bool ClipBackground => m_clipBackground;
}
