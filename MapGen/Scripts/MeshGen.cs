using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGen
{
    public static MeshData GenerateMesh(float[,] heightMap,float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        AnimationCurve meshHeightCurve = new AnimationCurve(heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1)/ -2f;
        float topLeftZ = (width - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail==0) ? 1 : levelOfDetail * 2;//if level of detail is set to 0 make it equal to 1 instead of multiplying it by two
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;


        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y+= meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x+=meshSimplificationIncrement)
            {

                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, meshHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);//toplefts allow the map to be centered 

                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y/(float)height);
                if (x < width -1 && y < height -1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex += 1;
            }
        }
        return meshData;//returns meshData for threading so that the game doesnt freeze up when we generate chunks
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    int triangleIndex;
    public Vector2[] uvs;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];//number of verts is the width * height
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6]; //the number of triangles, if confused draw out the vertices and draw the triangles

    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
