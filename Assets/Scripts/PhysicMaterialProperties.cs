using System;
using UnityEngine;

[Serializable]
public struct PhysicMaterialProperties
{
	[Range(0, 5)]
	public float friction;

	[Range(0, 1)]
	public float rollFriction;

	[Range(-1, 0.05f)]
	public float bumpMin;

	[Range(-0.2f, 1)]
	public float bumpMax;

	[Range(0, 100)]
	public float bumpScale;

	[Range(-1, 0)]
	public float bump2Min;

	[Range(0, 1)]
	public float bump2Max;

	[Range(-1, 100)]
	public float bump2Scale;
}