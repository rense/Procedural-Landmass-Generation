using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColorMap, Mesh, FalloffMap};
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData noiseData;

	[Range(0, 6)]
	public int editorPreviewLOD;

	public bool autoUpdate;

	public TerrainType[] regions;
	static MapGenerator instance;

	float[,] falloffMap;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Awake() {
		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	void OnValuesUpdated() {
		if(!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	public static int mapChunkSize {
		get {
			if(instance == null) {
				instance = FindObjectOfType<MapGenerator>();
			}
			if(!instance.terrainData.useFlatShading) {
				// vertices = ((width - 1) / i) + 1
				// limit for a square chunk is 65025 vertices: 255^2, so a max-width of 255
				// we use 241, because 240 (width - 1) is divisible by 2, 4, 6, 10, 12
				// minus 2 because of borderedSize
				return 239;
			} else {
				// alternative for flat-shading (96 - 1)
				return 95;
			}
		}

	}

	public void RequestMapData(Vector2 center, Action<MapData> callback) {
		ThreadStart threadStart = delegate { 
			MapDataThread(center, callback);
		};
		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 center, Action<MapData> callback) {
		MapData mapData = GenerateMapData(center);
		lock(mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate { 
			MeshDataThread(mapData, lod, callback);
		};
		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(
			mapData.heightMap, 
			terrainData.meshHeightMultiplier,
			terrainData.meshHeightCurve, 
			lod, 
			terrainData.useFlatShading
		);
		lock(meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update() {
		if(mapDataThreadInfoQueue.Count > 0) {
			for(int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
		if(meshDataThreadInfoQueue.Count > 0) {
			for(int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}

	MapData GenerateMapData(Vector2 center) {
		float[,] noiseMap = Noise.GenerateNoiseMap(
			mapChunkSize + 2, 
			mapChunkSize + 2, 
			noiseData.seed, 
			noiseData.noiseScale, 
			noiseData.octaves, 
			noiseData.persistance, 
			noiseData.lacunarity, 
			center + noiseData.offset, 
			noiseData.normalizeMode
		);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

		for(int y = 0; y < mapChunkSize; y++) {
			for(int x = 0; x < mapChunkSize; x++) {

				if(terrainData.useFalloff) {
					noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
				}

				float currentHeight = noiseMap[x, y];
				for(int i = 0; i < regions.Length; i++) {
					if(currentHeight >= regions[i].height) {
						colorMap[y * mapChunkSize + x] = regions[i].color;
					} else {
						break;
					}
				}
			}
		}
		return new MapData(noiseMap, colorMap);
	}

	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData(Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();
		if(drawMode == DrawMode.NoiseMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		} else if(drawMode == DrawMode.ColorMap) {
			display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.Mesh) {
			display.DrawMesh(
				MeshGenerator.GenerateTerrainMesh(
					mapData.heightMap, 
					terrainData.meshHeightMultiplier, 
					terrainData.meshHeightCurve, 
					editorPreviewLOD, 
					terrainData.useFlatShading),
				
				TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize)
			);
		} else if(drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));

		}
	}

	void OnValidate() {
		if(terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}
		if(noiseData != null) {
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}

		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color color;
}

public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;

	public MapData(float[,] heightMap, Color[] colorMap) {
		this.heightMap = heightMap;
		this.colorMap = colorMap;
	}
}
