using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessGen : MonoBehaviour
{
    // Start is called before the first frame update
    const float scale = 5f;
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    int chunkSize;
    int chunksVisible;


    public static float maxViewDist;
    public LODInfo[] detailLevels;


    public Transform viewer;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;


    static MapGen mapGen;
    public Material mapMaterial;


    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();


    private void Start()
    {
        mapGen = FindObjectOfType<MapGen>();
        maxViewDist = detailLevels [detailLevels.Length - 1].visibleDistanceThreshold;
        
        chunkSize = MapGen.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDist / chunkSize);
        UpdateVisibleChunks();
    }
    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) /scale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateCoordChunk();
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds; //used to find a point on the perimiter that is closest to another point

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        




        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();//we can do this because add component returns what it creates
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = positionV3 * scale;
            //meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);


            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i <detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateCoordChunk);
            }
            mapGen.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGen.TextureFromColorMap(mapData.colorMap, MapGen.mapChunkSize, MapGen.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateCoordChunk();
        }


        public void UpdateCoordChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistFromNearestEdge =Mathf.Sqrt( bounds.SqrDistance(viewerPosition));
                bool isVisible = viewerDistFromNearestEdge <= maxViewDist;

                if (isVisible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                        if (lodIndex != previousLODIndex)
                        {
                            LODMesh lodMesh = lodMeshes[lodIndex];
                            if (lodMesh.hasReceivedMesh)
                            {
                                previousLODIndex = lodIndex;
                                meshFilter.mesh = lodMesh.mesh;
                                meshCollider.sharedMesh = lodMesh.mesh;
                            }
                            else if (!lodMesh.hasRequestedMesh)
                            {
                                lodMesh.RequestMesh(mapData);

                            }
                        }
                        terrainChunksVisibleLastUpdate.Add(this);
                    }
                }
                SetVisible(isVisible);
            }
        }
        public void SetVisible(bool isVisible)
        {
            meshObject.SetActive(isVisible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
        
    }

    class LODMesh//responsible for fetching its own mesh for lower detail meshes
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasReceivedMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }
        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasReceivedMesh = true;

            updateCallback();
        }
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGen.RequestMeshData(mapData, lod,OnMeshDataReceived);
        }

    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }

}
