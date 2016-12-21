using UnityEngine;
using System.Collections;


[CreateAssetMenu()]
public class TextureData : UpdatableData {

	public Color[] baseColors;

	[Range(0,1)]
	public float[] baseStartHeights;

	[Range(0,1)]
	public float[] baseBlends;

	float savedMinHeight;
	float savedMaxHeight;


	public void ApplyToMaterial(Material material) {

		material.SetInt("baseColorCount", baseColors.Length);
		material.SetColorArray("baseColors", baseColors);
		material.SetFloatArray("baseStartHeights", baseStartHeights);
		material.SetFloatArray("baseBlends", baseBlends);

		UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
	}

	public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {

		savedMinHeight = minHeight;
		savedMaxHeight = maxHeight;

		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
	}
}
