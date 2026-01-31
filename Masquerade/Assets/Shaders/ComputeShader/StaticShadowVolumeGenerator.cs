using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;

public class StaticShadowVolumeGenerator : MonoBehaviour
{
    [SerializeField] private ComputeShader silhouetteComputeShader;
    [SerializeField] private Material shadowMaterial;

    // Debug Options
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugVisuals = false;
    [SerializeField] private bool showSilhouetteEdges = true;
    [SerializeField] private bool showExtrudedVolume = true;
    [SerializeField] private bool showLightConnection = true;
    [SerializeField] private Color silhouetteColor = Color.yellow;
    [SerializeField] private Color extrudedVolumeColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color lightConnectionColor = new Color(0f, 1f, 1f, 0.2f);
    [SerializeField] private float debugLineWidth = 2f;
    [SerializeField] private bool showStats = true;
    [SerializeField] private bool skinnedMesh = false;

    //private int threadGroupSize = 64;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private ComputeBuffer edgeBuffer;
    private ComputeBuffer edgeHashTable;
    private ComputeBuffer edgeHashCount;
    private ComputeBuffer edgeCountBuffer;
    private ComputeBuffer silhouetteEdgeListBuffer;
    private ComputeBuffer silhouetteEdgeCountBuffer;
    private ComputeBuffer shadowVolumeBuffer;
    private ComputeBuffer shadowVolumeIndexBuffer;
    private ComputeBuffer debugDotBuffer;
    private ComputeBuffer debugFrontBuffer;
    private ComputeBuffer debugBackBuffer;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private int kernelHandle;
    private uint threadGroupSize = 64;

    // Configuration parameters
    private int maxEdges = 65536; // Adjust based on your mesh complexity
    private int hashTableSize = 8192; // Should be a power of 2 for better hash distribution

    // Debug statistics
    private int silhouetteEdgeCount;
    private float computeTime;
    private Vector3[] debugVertices;
    private int[] debugIndices;

    private void Start()
    {
        // Get mesh filter component
        if (!skinnedMesh)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogError("No MeshFilter found!");
                enabled = false;
                return;
            }

            mesh = meshFilter.sharedMesh;
        } else
        {
            mesh = new Mesh();
            GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
        }

        

        // Initialize compute shader
        kernelHandle = silhouetteComputeShader.FindKernel("CSMain");
        Debug.Log(kernelHandle);

        silhouetteComputeShader.GetKernelThreadGroupSizes(silhouetteComputeShader.FindKernel("CSMain"), out threadGroupSize, out _, out _);

        // Create buffers
        CreateBuffers();
    }

    private void CreateBuffers()
    {
        if (mesh == null) return;
        int maxEdges = mesh.triangles.Length;  // Maximum possible edges

        // Create vertex buffer
        Vertex[] vertices = new Vertex[mesh.vertexCount];
        Vector3[] positions = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Vector2[] uvs = mesh.uv;

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vertices[i] = new Vertex
            {
                position = positions[i],
                normal = normals.Length > 0 ? normals[i] : Vector3.up,
                tangent = tangents.Length > 0 ? tangents[i] : new Vector4(1, 0, 0, 1),
                uv = uvs.Length > 0 ? uvs[i] : Vector2.zero
            };
        }

        vertexBuffer = new ComputeBuffer(vertices.Length, System.Runtime.InteropServices.Marshal.SizeOf<Vertex>());
        vertexBuffer.SetData(vertices);

        // Create index buffer
        indexBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        indexBuffer.SetData(mesh.triangles);

        // Create edge detection buffers
        edgeBuffer = new ComputeBuffer(maxEdges, System.Runtime.InteropServices.Marshal.SizeOf<Edge>());

        // IMPORTANT: Update the hash table structure
        edgeHashTable = new ComputeBuffer(hashTableSize, sizeof(uint) * 2);

        // Add this line - it was missing
        edgeHashCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        edgeCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        silhouetteEdgeListBuffer = new ComputeBuffer(maxEdges * 2, sizeof(int));
        silhouetteEdgeCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        // Create shadow volume buffers
        shadowVolumeBuffer = new ComputeBuffer(maxEdges * 4, sizeof(float) * 3);
        shadowVolumeIndexBuffer = new ComputeBuffer(maxEdges * 6, sizeof(int));

        // Initialize counter buffers to zero
        int[] zero = new int[] { 0 };
        edgeCountBuffer.SetData(zero);
        edgeHashCount.SetData(zero);
        silhouetteEdgeCountBuffer.SetData(zero);

        // Initialize hash table with invalid entries (all bits set to 1)
        uint[] invalidEntries = new uint[hashTableSize * 2];
        for (int i = 0; i < invalidEntries.Length; i++)
        {
            invalidEntries[i] = 0xFFFFFFFF;
        }
        edgeHashTable.SetData(invalidEntries);

        //Initialize debug buffers
        debugDotBuffer = new ComputeBuffer(maxEdges, sizeof(float));
        debugFrontBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        debugBackBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }



    private void Update()
    {
        if (!enabled || mesh == null) return;

        Light nearestLight = FindNearestLight();
        if (nearestLight == null) return;

        // Dispatch compute shader
        DispatchComputeShader(nearestLight);

        // Render shadow volume
        RenderShadowVolume();

        // Simple debug to confirm visualization works
        if (showDebugVisuals)
        {
            Debug.DrawLine(Vector3.zero, Vector3.forward * 5, Color.red);
            Debug.DrawLine(Vector3.zero, Vector3.right * 5, Color.green);
            Debug.DrawLine(Vector3.zero, Vector3.up * 5, Color.blue);
        }
    }

    private Light FindNearestLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        float nearestDistance = float.MaxValue;
        Light nearestLight = null;

        foreach (Light light in lights)
        {
            if (light.type != LightType.Point && light.type != LightType.Spot) continue;

            float distance = Vector3.Distance(transform.position, light.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLight = light;
            }
        }

        return nearestLight;
    }

    private void DispatchComputeShader(Light light)
    {
        float startTime = Time.realtimeSinceStartup;

        // Reset counters before dispatching
        int[] zero = new int[] { 0 };
        edgeCountBuffer.SetData(zero);
        silhouetteEdgeCountBuffer.SetData(zero);

        // Get kernel handles
        int edgeDetectionKernel = silhouetteComputeShader.FindKernel("EdgeDetection");
        int silhouetteKernel = silhouetteComputeShader.FindKernel("CSMain");
        int shadowVolumeKernel = silhouetteComputeShader.FindKernel("ShadowVolumeGeneration");

        // Set common parameters for all kernels - note the matrix conversion!
        Matrix4x4 localToWorld = transform.localToWorldMatrix;
        silhouetteComputeShader.SetMatrix("_ObjectToWorld", localToWorld);

        // The light position and direction need to be in world space
        silhouetteComputeShader.SetVector("_LightPosition", light.transform.position);
        silhouetteComputeShader.SetVector("_LightDirection", light.transform.forward);
        silhouetteComputeShader.SetFloat("_LightSpotAngle", light.spotAngle);
        silhouetteComputeShader.SetFloat("_LightRange", light.range);
        silhouetteComputeShader.SetInt("_LightType", light.type == LightType.Point ? 0 : 1);
        silhouetteComputeShader.SetInt("_VertexCount", mesh.vertexCount);
        silhouetteComputeShader.SetInt("_IndexCount", mesh.triangles.Length);
        silhouetteComputeShader.SetFloat("_ExtrusionLength", 100f); // Try a larger extrusion for visibility
        silhouetteComputeShader.SetInt("_MaxEdges", maxEdges);
        silhouetteComputeShader.SetInt("_HashTableSize", hashTableSize);

        // Set common buffers and parameters for all kernels
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_VertexBuffer", vertexBuffer);
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_IndexBuffer", indexBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_VertexBuffer", vertexBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_IndexBuffer", indexBuffer);
        silhouetteComputeShader.SetBuffer(shadowVolumeKernel, "_VertexBuffer", vertexBuffer);
        silhouetteComputeShader.SetBuffer(shadowVolumeKernel, "_ShadowVolumeBuffer", shadowVolumeBuffer);
        silhouetteComputeShader.SetBuffer(shadowVolumeKernel, "_ShadowVolumeIndexBuffer", shadowVolumeIndexBuffer);

        // Set edge detection specific buffers
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_EdgeBuffer", edgeBuffer);
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_EdgeHashBuffer", edgeHashTable);
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_EdgeHashCount", edgeHashCount);
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_EdgeCount", edgeCountBuffer);
        silhouetteComputeShader.SetBuffer(edgeDetectionKernel, "_SilhouetteEdgeCount", silhouetteEdgeCountBuffer);


        // Set silhouette detection specific buffers
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_EdgeBuffer", edgeBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_EdgeHashBuffer", edgeHashTable);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_EdgeHashCount", edgeHashCount);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_EdgeCount", edgeCountBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_SilhouetteEdgeList", silhouetteEdgeListBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_SilhouetteEdgeCount", silhouetteEdgeCountBuffer);

        // Set shadow volume generation specific buffers
        silhouetteComputeShader.SetBuffer(shadowVolumeKernel, "_SilhouetteEdgeList", silhouetteEdgeListBuffer);
        silhouetteComputeShader.SetBuffer(shadowVolumeKernel, "_SilhouetteEdgeCount", silhouetteEdgeCountBuffer);

        //Set debug buffers
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_DebugDotProducts", debugDotBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_DebugFrontCount", debugFrontBuffer);
        silhouetteComputeShader.SetBuffer(silhouetteKernel, "_DebugBackCount", debugBackBuffer);



        // Dispatch kernels in sequence
        // 1. Edge detection - process triangles
        int triangleCount = mesh.triangles.Length / 3;
        int edgeThreadGroups = Mathf.CeilToInt(triangleCount / 64.0f);
        silhouetteComputeShader.Dispatch(edgeDetectionKernel, edgeThreadGroups, 1, 1);

        // 2. Silhouette detection - process edges
        int[] edgeCountArray = new int[1];
        edgeCountBuffer.GetData(edgeCountArray);
        int actualEdgeCount = edgeCountArray[0];
        int edgeThreadGroupsActual = Mathf.CeilToInt(actualEdgeCount / 64.0f);

        // Use actual edge count or max, whichever is smaller
        int edgeThreadGroupsToUse = Mathf.Min(edgeThreadGroupsActual, Mathf.CeilToInt(maxEdges / 64.0f));
        silhouetteComputeShader.Dispatch(silhouetteKernel, edgeThreadGroupsToUse, 1, 1);

        // 3. Shadow volume generation - process silhouette edges
        int[] silhouetteCountArray = new int[1];
        silhouetteEdgeCountBuffer.GetData(silhouetteCountArray);
        int silhouetteCount = silhouetteCountArray[0];
        int silhouetteThreadGroups = Mathf.CeilToInt(silhouetteCount / 64.0f);

        // Only dispatch if we have silhouette edges
        if (silhouetteCount > 0)
        {
            silhouetteComputeShader.Dispatch(shadowVolumeKernel, silhouetteThreadGroups, 1, 1);
            Debug.Log($"Found {silhouetteCount} silhouette edges");
        }
        else
        {
            Debug.LogWarning("No silhouette edges detected from this viewpoint");
        }

        DebugEdgeDetection();
        computeTime = Time.realtimeSinceStartup - startTime;
    }

    private void RenderShadowVolume()
    {
        // Create a temporary mesh for rendering
        Mesh shadowMesh = new Mesh();
        shadowMesh.indexFormat = IndexFormat.UInt32;

        // Get silhouette edge count first to allocate proper arrays
        int[] countData = new int[1];
        silhouetteEdgeCountBuffer.GetData(countData);
        silhouetteEdgeCount = countData[0];

        // Skip processing if no silhouette edges were found
        if (silhouetteEdgeCount <= 0)
        {
            Debug.Log("No silhouette edges detected");
            return;
        }

        // Allocate arrays with the correct size based on actual data
        Vector3[] vertices = new Vector3[silhouetteEdgeCount * 4];
        int[] indices = new int[silhouetteEdgeCount * 6];

        // Get data from compute buffers - only read the actual data we need
        shadowVolumeBuffer.GetData(vertices, 0, 0, silhouetteEdgeCount * 4);
        shadowVolumeIndexBuffer.GetData(indices, 0, 0, silhouetteEdgeCount * 6);

        // Store for debug visualization
        debugVertices = vertices;
        debugIndices = indices;

        // Check if volumes are centered at origin (indicates a problem)
        bool allAtOrigin = true;
        for (int i = 0; i < vertices.Length && i < 20; i++)
        { // Check first few vertices
            if (vertices[i].magnitude > 0.01f)
            {
                allAtOrigin = false;
                break;
            }
        }

        if (allAtOrigin)
        {
            Debug.LogWarning("All shadow volume vertices appear to be at origin - check transform matrices");
        }

        // Set the mesh data directly - the compute shader already generated proper indices
        shadowMesh.SetVertices(vertices);
        shadowMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        shadowMesh.RecalculateBounds();

        debugVertices = vertices;
        debugIndices = indices;
        Debug.Log(vertices.Length);

        // Draw the shadow volume
        if (shadowMaterial != null)
        {
            // Use matrix.identity because vertices are already in world space
            Graphics.DrawMesh(shadowMesh, Matrix4x4.identity, shadowMaterial, 0);
        }
    }

    private void OnDestroy()
    {
        vertexBuffer?.Release();
        indexBuffer?.Release();
        edgeBuffer?.Release();
        edgeHashTable?.Release();
        edgeCountBuffer?.Release();
        silhouetteEdgeListBuffer?.Release();
        silhouetteEdgeCountBuffer?.Release();
        shadowVolumeBuffer?.Release();
        shadowVolumeIndexBuffer?.Release();
        debugDotBuffer?.Release();
        debugFrontBuffer?.Release();
        debugBackBuffer?.Release();
        edgeHashCount?.Release();
    }

    // Struct matching compute shader
    private struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv;
    }

    private struct Edge
    {
        public uint v0;
        public uint v1;
        public uint triangleIndex;
        public int isSilhouette; // Using int for bool (0 = false, 1 = true)
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || !Application.isPlaying || debugVertices == null) return;
        Light nearestLight = FindNearestLight();
        if (nearestLight == null) return;

        // Get actual count data
        int[] countData = new int[1];
        if (silhouetteEdgeCountBuffer != null)
        {
            silhouetteEdgeCountBuffer.GetData(countData);
            silhouetteEdgeCount = countData[0];
        }

        // Draw silhouette edges
        if (showSilhouetteEdges)
        {
            Gizmos.color = silhouetteColor;
            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 1 < debugVertices.Length)
                {
                    // Check if vertices are valid (not at origin)
                    if (debugVertices[baseIndex].magnitude > 0.01f &&
                        debugVertices[baseIndex + 1].magnitude > 0.01f)
                    {
                        DrawThickLine(
                            debugVertices[baseIndex],
                            debugVertices[baseIndex + 1],
                            debugLineWidth,
                            silhouetteColor
                        );

                        Gizmos.DrawSphere(debugVertices[baseIndex], debugLineWidth * 0.1f);
                        Gizmos.DrawSphere(debugVertices[baseIndex + 1], debugLineWidth * 0.1f);
                    }
                }
            }
        }

        // Draw extruded volume
        if (showExtrudedVolume)
        {
            Gizmos.color = extrudedVolumeColor;
            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 3 < debugVertices.Length)
                {
                    DrawThickLine(
                        debugVertices[baseIndex],
                        debugVertices[baseIndex + 1],
                        debugLineWidth,
                        extrudedVolumeColor
                    );
                    DrawThickLine(
                        debugVertices[baseIndex + 2],
                        debugVertices[baseIndex + 3],
                        debugLineWidth,
                        extrudedVolumeColor
                    );
                    DrawThickLine(
                        debugVertices[baseIndex],
                        debugVertices[baseIndex + 2],
                        debugLineWidth,
                        extrudedVolumeColor
                    );
                    DrawThickLine(
                        debugVertices[baseIndex + 1],
                        debugVertices[baseIndex + 3],
                        debugLineWidth,
                        extrudedVolumeColor
                    );

                    Gizmos.DrawSphere(debugVertices[baseIndex + 2], debugLineWidth * 0.1f);
                    Gizmos.DrawSphere(debugVertices[baseIndex + 3], debugLineWidth * 0.1f);
                }
            }
        }

        // Draw light connections
        if (showLightConnection)
        {
            Gizmos.color = lightConnectionColor;
            Vector3 lightPos = nearestLight.transform.position;

            Gizmos.DrawWireSphere(lightPos, debugLineWidth * 0.5f);

            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 1 < debugVertices.Length)
                {
                    DrawThickLine(
                        lightPos,
                        debugVertices[baseIndex],
                        debugLineWidth / 4f,
                        lightConnectionColor
                    );
                    DrawThickLine(
                        lightPos,
                        debugVertices[baseIndex + 1],
                        debugLineWidth / 4f,
                        lightConnectionColor
                    );
                }
            }
        }
    }

    // Add this to your script to debug the compute shader
    private void DebugEdgeDetection()
    {
        // Read edge count
        int[] edgeCount = new int[1];
        edgeCountBuffer.GetData(edgeCount);

        Debug.Log($"Total edges detected: {edgeCount[0]}");

        // Read silhouette edge count
        int[] silEdgeCount = new int[1];
        silhouetteEdgeCountBuffer.GetData(silEdgeCount);

        Debug.Log($"Silhouette edges detected: {silEdgeCount[0]}");

        // Check if we have any edges
        if (edgeCount[0] > 0)
        {
            // Read a sample of edges from the buffer
            Edge[] edges = new Edge[Mathf.Min(edgeCount[0], 10)];
            edgeBuffer.GetData(edges, 0, 0, edges.Length);

            // Display first few edges
            for (int i = 0; i < edges.Length; i++)
            {
                Debug.Log($"Edge {i}: v0={edges[i].v0}, v1={edges[i].v1}, triangle={edges[i].triangleIndex}, isSilhouette={edges[i].isSilhouette}");
            }
        }
    }

    private void DrawThickLine(Vector3 start, Vector3 end, float width, Color color)
    {
#if UNITY_EDITOR
        Vector3 cameraRight = SceneView.currentDrawingSceneView?.camera?.transform.right ?? Vector3.right;

        Vector3 forward = (end - start).normalized;
        Vector3 right = Vector3.Cross(forward, cameraRight).normalized * width * 0.5f;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = start - right;
        vertices[1] = start + right;
        vertices[2] = end - right;
        vertices[3] = end + right;

        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAConvexPolygon(vertices[0], vertices[1], vertices[2]);
        Handles.DrawAAConvexPolygon(vertices[1], vertices[3], vertices[2]);
        Handles.EndGUI();
#endif
    }

    private void OnGUI()
    {
        if (!showStats || !showDebugVisuals || !Application.isPlaying) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(10, 10, 10, 10);

        GUI.Box(new Rect(10, 10, 200, 100), string.Format(
            "Shadow Volume Stats:\n" +
            "Silhouette Edges: {0}\n" +
            "Vertices Generated: {1}\n" +
            "Triangles Generated: {2}\n" +
            "Compute Time: {3:F2}ms",
            silhouetteEdgeCount,
            debugVertices?.Length ?? 0,
            debugIndices?.Length / 3 ?? 0,
            computeTime * 1000f
        ), style);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure debug line width stays reasonable
        debugLineWidth = Mathf.Clamp(debugLineWidth, 0.1f, 10f);
    }
#endif
}