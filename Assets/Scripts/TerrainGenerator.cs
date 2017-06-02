using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class TerrainGenerator : MonoBehaviour {

	const float viewerMoveTresholdForChunkUpdate = 25f;
	const float sqrViewerMoveTresholdForChunkUpdate = viewerMoveTresholdForChunkUpdate * viewerMoveTresholdForChunkUpdate;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public Material terrainMaterial;

	Vector2 viewerPosition;
	Vector2 viewerPositionOld;

	float meshWorldSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, TerrainChunk> chunks = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

	void Start() {

		textureSettings.ApplyToMaterial(terrainMaterial);
		textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceTreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);
	
		UpdateVisibleChunks();
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		if(viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}
	
		if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveTresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	void UpdateVisibleChunks() {

		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();

		for (int i = visibleChunks.Count - 1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add (visibleChunks [i].coord);
			visibleChunks [i].UpdateTerrainChunk ();
		}

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2(
					currentChunkCoordX + xOffset, currentChunkCoordY + yOffset
				);
				if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord)) {
					if (chunks.ContainsKey (viewedChunkCoord)) {
						chunks [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						TerrainChunk chunk = new TerrainChunk (
							viewedChunkCoord, 
							heightMapSettings, 
							meshSettings,
							detailLevels, 
							colliderLODIndex, 
							transform,
							viewer,
							terrainMaterial
						);
						chunks.Add (viewedChunkCoord, chunk);
						chunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
						chunk.Load ();
					}
				}
			}
		}
	}
		
	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleChunks.Add (chunk);
		} else {
			visibleChunks.Remove (chunk);
		}
	}
}

[System.Serializable]
public struct LODInfo {

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int lod;
	public float visibleDistanceTreshold;

	public float sqrVisibleDistanceTreshold {
		get {
			return visibleDistanceTreshold * visibleDistanceTreshold;
		}
	}
}