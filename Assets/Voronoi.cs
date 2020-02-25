using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voronoi : MonoBehaviour
{
    // Parameters
    public int nPoints = 10;
    public int nHeightPoints = 10;
    public int squareSize = 500;
    float maxHeight = 40.0f;
    float noise = 0.2f;

    // Main voronoi variables
    public List<Vector2Int> points;
    float[,] distances;
    float maxDist = 0;
    Texture2D detailVoronoiTexture;

    // Height changing voronoi
    public List<Vector2Int> heightPoints;
    float[,] heightDistances;
    float heightMaxDist = 0;
    Texture2D generalVoronoiTexture;

    Texture2D finalVoronoiTexture;
    Texture2D finalVoronoiColor;

    // 3D Mesh variables
    public Material material;
    public GameObject meshObject;
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    // Sprite visualization variables
    public GameObject spriteVoronoi;
    public GameObject spriteHeight;
    public GameObject spriteFinal;
    public GameObject spriteColor;

    void Start()
    {
        points = new List<Vector2Int>(nPoints);
        detailVoronoiTexture = createVoronoi(ref points, nPoints, ref distances, ref maxDist);
        generalVoronoiTexture = createVoronoi(ref heightPoints, nHeightPoints, ref heightDistances, ref heightMaxDist);
        finalVoronoiTexture = new Texture2D(squareSize, squareSize);
        finalVoronoiColor = new Texture2D(squareSize, squareSize);

        // Multiply voronoi and height to create final heightmap texture
        for (int i = 0; i < squareSize; i++)
        {
            for (int j = 0; j < squareSize; j++)
            {
                float value = detailVoronoiTexture.GetPixel(i, j).r * generalVoronoiTexture.GetPixel(i, j).r;
                finalVoronoiTexture.SetPixel(i, j, new Color(value, value, value));

                Color pixelColor = getEnvironmentColorFromHeight(value);
                finalVoronoiColor.SetPixel(i, j, new Color(pixelColor.r, pixelColor.g, pixelColor.b));
            }
        }
        finalVoronoiTexture.Apply();
        finalVoronoiColor.Apply();

        //Show it
        spriteVoronoi.GetComponent<SpriteRenderer>().sprite = Sprite.Create(detailVoronoiTexture, new Rect(0, 0, squareSize, squareSize), Vector2.one * 0.5f);
        spriteHeight.GetComponent<SpriteRenderer>().sprite = Sprite.Create(generalVoronoiTexture, new Rect(0, 0, squareSize, squareSize), Vector2.one * 0.5f);
        spriteFinal.GetComponent<SpriteRenderer>().sprite = Sprite.Create(finalVoronoiTexture, new Rect(0, 0, squareSize, squareSize), Vector2.one * 0.5f);
        spriteColor.GetComponent<SpriteRenderer>().sprite = Sprite.Create(finalVoronoiColor, new Rect(0, 0, squareSize, squareSize), Vector2.one * 0.5f);

        //Create mesh
        mesh = new Mesh();
        meshObject.GetComponent<MeshFilter>().mesh = mesh;
        CreateMesh(finalVoronoiTexture);
        UpdateMesh();

        //Export images
        ExportImage(detailVoronoiTexture, "detailVoronoi.png");
        ExportImage(generalVoronoiTexture, "generalVoronoiTexture.png");
        ExportImage(finalVoronoiTexture, "finalHeightMap.png");
        ExportImage(finalVoronoiColor, "finalColor.png");
    }

    void Update()
    {
        
    }

    // ------------------- Create Voronoi -------------------
    Texture2D createVoronoi(ref List<Vector2Int> points, int nPoints, ref float[,] distances, ref float maxDist)
    {
        //Generate random points
        for (int i = 0; i < nPoints; i++)
        {
            int x = Random.Range(0, squareSize);
            int y = Random.Range(0, squareSize);

            points.Add(new Vector2Int(x, y));
        }

        //Calculate values
        distances = new float[squareSize, squareSize];

        for (int i = 0; i < squareSize; i++)
        {
            for (int j = 0; j < squareSize; j++)
            {
                distances[i, j] = distanceToClosestPoint(i, j, points);
            }
        }

        //normalize values
        maxDist = findMax(distances);

        //create texture
        Texture2D tex = new Texture2D(squareSize, squareSize);
        for (int i = 0; i < squareSize; i++)
        {
            for (int j = 0; j < squareSize; j++)
            {
                float colorVal = distances[i, j] / maxDist;
                tex.SetPixel(i, j, new Color(colorVal, colorVal, colorVal));
            }
        }
        tex.Apply();
        return tex;
    }

    // ------------------- Helper functions -------------------
    int distanceToClosestPoint(int x, int y, List<Vector2Int> points)
    {
        float closest = 99999;
        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), points[i]);
            if (distance < closest)
                closest = distance;
        }
        return (int)closest;
    }

    float findMax(float[,] matrix)
    {
        float max = 0;
        foreach (float val in matrix)
            if (val > max)
                max = val;
        return max;
    }

    void CreateMesh(Texture2D voronoi)
    {
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        vertices = new Vector3[squareSize * squareSize];
        

        // create vertices
        for (int i = 0; i < squareSize; i++)
        {
            for (int j = 0; j < squareSize; j++)
            {
                float height = voronoi.GetPixel(i, j).r * maxHeight + Random.Range(-noise, noise);
                vertices[i * squareSize + j] = new Vector3(i, height, j);
            }
        }

        // create triangles
        triangles = new int[(squareSize-1) * (squareSize-1) * 3 * 2]; //{ 0, 1, 501 };
        int triangleIndex = 0;
        for (int i = 0; i < squareSize - 1; i++)
        {
            for (int j = 0; j < squareSize - 1; j++)
            {
                triangles[triangleIndex++] = j + i * squareSize; //0
                triangles[triangleIndex++] = j + i * squareSize + 1; //1
                triangles[triangleIndex++] = j + i * squareSize + squareSize; //500

                triangles[triangleIndex++] = j + i * squareSize + 1; //1
                triangles[triangleIndex++] = j + i * squareSize + squareSize + 1; //501
                triangles[triangleIndex++] = j + i * squareSize + squareSize; //500

            }
        }

        //Debug.Log(triangleIndex);
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // create uvs
        uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / squareSize, vertices[i].z / squareSize);
        }
        mesh.uv = uvs;

        material.SetTexture(Shader.PropertyToID("_MainTex"), finalVoronoiColor);
        mesh.RecalculateNormals();
    }

    Color getEnvironmentColorFromHeight(float height)
    {
        //percentages
        float snow = 100.0f;
        float mountain = 70.0f;
        float valley = 25.0f;
        //float beach = 5.0f;

        Color color = new Color();
        float correctedHeight = height * 100.0f;

        ////if (correctedHeight < beach)
        ////{
        ////    color.r = 0.8f;
        ////    color.g = 0.8f;
        ////    color.b = 0.1f;
        ////} else

        float factor = correctedHeight / 50.0f + Random.Range(-0.1f, 0.1f);
        if (correctedHeight < valley)
        {
            color.r = 0.6f - 0.5f * factor;
            color.g = 0.9f - 0.5f * factor;
            color.b = 0.2f;
        }
        else if (correctedHeight < mountain)
        {
            color.r = 0.6f * factor;
            color.g = 0.6f * factor;
            color.b = 0.6f * factor;
        }
        else if (correctedHeight <= snow)
        {
            color.r = 0.9f;
            color.g = 0.9f;
            color.b = 0.9f;
        }



        return color;
    }

    void ExportImage(Texture2D texture, string fileName)
    {
        string path = "./OutputTextures/" + fileName;
        byte[] _bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + path);
    }
}
