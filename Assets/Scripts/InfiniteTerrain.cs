using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InfiniteTerrain : MonoBehaviour {
	
	public const float maxViewDistance = 500;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	static MapGenerator mapGenerator;

	int chunkSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
	List<Chunk> chunksVisibleLastUpdate = new List<Chunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator>();

		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
	}

	void UpdateVisibleChunks() {

		for (int i = 0; i < chunksVisibleLastUpdate.Count; i++) {
			chunksVisibleLastUpdate[i].SetVisible(false);
		}
		chunksVisibleLastUpdate.Clear();

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2(
					currentChunkCoordX + xOffset, currentChunkCoordY + yOffset
				);

				if(chunks.ContainsKey(viewedChunkCoord)) {
					chunks[viewedChunkCoord].UpdateTerrainChunk();
					if(chunks[viewedChunkCoord].IsVisible()) {
						chunksVisibleLastUpdate.Add(chunks[viewedChunkCoord]);
					}
				} else {
					chunks.Add(viewedChunkCoord, new Chunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
				}
			}
		}
	}
		
	public class Chunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MapData mapData;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;


		public Chunk(Vector2 coord, int size, Transform parent, Material material) {
			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
		
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3;
			meshObject.transform.parent = parent;
			SetVisible(false);

			mapGenerator.RequestMapData(OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
		}

		void OnMeshDataReceived(MeshData meshData) {
			meshFilter.mesh = meshData.CreateMesh();
		}

		public void UpdateTerrainChunk() {
			float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
			SetVisible(visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}

}
