using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public class MapGen : MonoBehaviour
{
    public enum DrawMode { NoiseMap,ColorMap,Mesh};
    public DrawMode drawMode;

    public const int mapChunkSize = 241; // max size that is a factor of 8 since max verts can be 65025 per mesh. 
    [Range(0,6)]
    public int editorPreviewLOD;//will be 1 if no simplification the higher this is the lower the detail
    public float noiseScale;
 
    
    
    public int octaves;
    [Range(0,1)]
    public float persistance;// frequency  how close together/detailed
    public float lacunarity;//amplitude can be thought of as the size of the hills how large the difference between the tip and trough

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;//to better control the distribution of values on the height map.

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();




    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenerateMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }


    public void RequestMapData(Vector2 center, Action<MapData> callback)//threading
    {
        ThreadStart threadStart = delegate// represents mapdata thread with callback paramenter
        {
            MapDataThread(center, callback); 
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center,Action<MapData> callback)//method runs on seperate thread
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)//when one thread reaches this point no other thread can acces this code
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }


    public void RequestMeshData(MapData mapData,int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod,callback);
        };
        new Thread(threadStart).Start();
    }

    public void MeshDataThread(MapData mapData,int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGen.GenerateMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }

    }


    private void Update()
    {
        if(mapDataThreadInfoQueue.Count >0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }

        }
        if(meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = NoiseGen.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset);


        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight<= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);

    }





    private void OnValidate()//called whenever the script variable is changed in the inspector.
    {
        if (lacunarity<=1)
        {
            lacunarity = 1;
        }
        if (octaves<=1)
        {
            octaves = 1;
        }
    }

    struct MapThreadInfo<T>//generic for both 2Dmap and 3Dmesh
    {
        public readonly Action<T> callback;// readonly since structs should be immutable
        public readonly T parameter;
        public MapThreadInfo (Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}





[System.Serializable]
public struct TerrainType// struct for adding color to the noise map
{
    public string name;//not read only since it wont be visible in the editor
    public float height;
    // value for the height in which the color is placed
    //example in a two element array with element 0, with a height of .4 and element 2 with a height of 1
    //0-.4 will be element 1's color and .4-1 will be element 2's color
    public Color color;
}






public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)//constructor
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }

}
