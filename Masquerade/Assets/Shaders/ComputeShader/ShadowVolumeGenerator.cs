using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ShadowVolumeGenerator : MonoBehaviour
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

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private ComputeBuffer boneMatricesBuffer;
    private ComputeBuffer skinningDataBuffer;
    private ComputeBuffer shadowVolumeBuffer;
    private ComputeBuffer shadowVolumeIndexBuffer;

    private Mesh mesh;
    private bool isSkinnedMesh;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private MeshFilter meshFilter;
    private int kernelHandle;
    private uint threadGroupSize;

    // Debug statistics
    private int silhouetteEdgeCount;
    private float computeTime;
    private Vector3[] debugVertices;
    private int[] debugIndices;

    private void Start()
    {
        // Get components
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        isSkinnedMesh = skinnedMeshRenderer != null && skinnedMeshRenderer.enabled;

        // Get mesh data
        if (isSkinnedMesh)
        {
            mesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(mesh);
        }
        else if (meshFilter != null)
        {
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            Debug.LogError("No MeshFilter or SkinnedMeshRenderer found!");
            enabled = false;
            return;
        }

        // Initialize compute shader
        kernelHandle = silhouetteComputeShader.FindKernel("CSMain");
        silhouetteComputeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSize, out _, out _);

        // Create buffers
        CreateBuffers();
    }

    private void CreateBuffers()
    {
        if (mesh == null) return;
        int maxEdges = mesh.triangles.Length;  // Maximum possible edges
        

        // Create vertex buffer
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Vector2[] uvs = mesh.uv;

        Vertex[] vertexData = new Vertex[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexData[i] = new Vertex
            {
                position = vertices[i],
                normal = normals[i],
                tangent = tangents[i],
                uv = uvs[i]
            };
        }
        vertexBuffer = new ComputeBuffer(vertexData.Length, sizeof(float) * (3 + 3 + 4 + 2));
        vertexBuffer.SetData(vertexData);

        // Create index buffer
        int[] indices = mesh.triangles;
        indexBuffer = new ComputeBuffer(indices.Length, sizeof(int));
        indexBuffer.SetData(indices);

        // Create skinning buffers if needed
        if (isSkinnedMesh)
        {
            CreateSkinningBuffers();
        }
        else
        {
            // Create empty bone matrices buffer to avoid errors
            boneMatricesBuffer = new ComputeBuffer(1, sizeof(float) * 16);
            Matrix4x4[] emptyMatrix = new Matrix4x4[] { Matrix4x4.identity };
            boneMatricesBuffer.SetData(emptyMatrix);

            // Create empty skinning buffer
            skinningDataBuffer = new ComputeBuffer(1, sizeof(float) * 8);
            SkinningData[] emptySkinData = new SkinningData[] { new SkinningData() };
            skinningDataBuffer.SetData(emptySkinData);
        }

        // Create output buffers (estimate max size based on mesh complexity)
        int maxPossibleEdges = indices.Length;  // This is an upper bound
        shadowVolumeBuffer = new ComputeBuffer(maxEdges * 4, sizeof(float) * 3);
        shadowVolumeIndexBuffer = new ComputeBuffer(maxEdges * 6, sizeof(int));

        Matrix4x4[] defaultBoneMatrices = new Matrix4x4[] { Matrix4x4.identity };
        int boneMatrixCount = isSkinnedMesh ? Mathf.Max(skinnedMeshRenderer.bones.Length, 1) : 1;
        boneMatricesBuffer = new ComputeBuffer(boneMatrixCount, sizeof(float) * 16);
        boneMatricesBuffer.SetData(defaultBoneMatrices);

        // Always create skinning buffer with at least one element
        SkinningData[] defaultSkinningData = new SkinningData[] { new SkinningData() };
        int skinningDataCount = isSkinnedMesh ? mesh.vertexCount : 1;
        skinningDataBuffer = new ComputeBuffer(skinningDataCount, sizeof(float) * 8);
        skinningDataBuffer.SetData(defaultSkinningData);

        if (isSkinnedMesh)
        {
            UpdateSkinningBuffers();
        }
    }

    private void UpdateSkinningBuffers()
    {
        if (!isSkinnedMesh || skinnedMeshRenderer == null) return;

        Transform[] bones = skinnedMeshRenderer.bones;
        Matrix4x4[] bindposes = skinnedMeshRenderer.sharedMesh.bindposes;
        BoneWeight[] weights = skinnedMeshRenderer.sharedMesh.boneWeights;

        // Update bone matrices
        Matrix4x4[] boneMatrices = new Matrix4x4[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            boneMatrices[i] = bones[i].localToWorldMatrix * bindposes[i];
        }
        boneMatricesBuffer.SetData(boneMatrices);

        // Update skinning data
        SkinningData[] skinningData = new SkinningData[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            skinningData[i] = new SkinningData
            {
                boneWeights = new Vector4(weights[i].weight0, weights[i].weight1, weights[i].weight2, weights[i].weight3),
                boneIndices = new Vector4(weights[i].boneIndex0, weights[i].boneIndex1, weights[i].boneIndex2, weights[i].boneIndex3)
            };
        }
        skinningDataBuffer.SetData(skinningData);
    }

    private void CreateSkinningBuffers()
    {
        if (skinnedMeshRenderer == null) return;

        Transform[] bones = skinnedMeshRenderer.bones;
        Matrix4x4[] bindposes = skinnedMeshRenderer.sharedMesh.bindposes;
        BoneWeight[] weights = skinnedMeshRenderer.sharedMesh.boneWeights;

        // Create bone matrices buffer
        boneMatricesBuffer = new ComputeBuffer(Mathf.Max(bones.Length, 1), sizeof(float) * 16);
        Matrix4x4[] boneMatrices = new Matrix4x4[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            Debug.Log("bone values: " + boneMatrices.Length + ", " + bones.Length + ", " + bindposes.Length);
            boneMatrices[i] = bones[i].localToWorldMatrix * bindposes[i];
        }
        boneMatricesBuffer.SetData(boneMatrices);

        // Create skinning data buffer
        SkinningData[] skinningData = new SkinningData[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            skinningData[i] = new SkinningData
            {
                boneWeights = new Vector4(weights[i].weight0, weights[i].weight1, weights[i].weight2, weights[i].weight3),
                boneIndices = new Vector4(weights[i].boneIndex0, weights[i].boneIndex1, weights[i].boneIndex2, weights[i].boneIndex3)
            };
        }
        skinningDataBuffer = new ComputeBuffer(skinningData.Length, sizeof(float) * 8);
        skinningDataBuffer.SetData(skinningData);
    }

    private void Update()
    {
        if (!enabled || mesh == null) return;

        Light nearestLight = FindNearestLight();
        if (nearestLight == null) return;

        // Update skinning data if needed
        if (isSkinnedMesh)
        {
            UpdateSkinningBuffers(); // Replace UpdateBoneMatrices() with this
        }

        // Dispatch compute shader
        DispatchComputeShader(nearestLight);

        // Render shadow volume
        RenderShadowVolume();
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

    private void UpdateBoneMatrices()
    {
        if (!isSkinnedMesh || skinnedMeshRenderer == null) return;

        Transform[] bones = skinnedMeshRenderer.bones;
        Matrix4x4[] boneMatrices = new Matrix4x4[bones.Length];
                
        for (int i = 0; i < bones.Length; i++)
        {
            boneMatrices[i] = bones[i].localToWorldMatrix * skinnedMeshRenderer.sharedMesh.bindposes[i];
        }

        boneMatricesBuffer.SetData(boneMatrices);
    }

    private void DispatchComputeShader(Light light)
    {
        

        float startTime = Time.realtimeSinceStartup;


        // Set buffers
        silhouetteComputeShader.SetBuffer(kernelHandle, "_VertexBuffer", vertexBuffer);
        silhouetteComputeShader.SetBuffer(kernelHandle, "_IndexBuffer", indexBuffer);
        silhouetteComputeShader.SetBuffer(kernelHandle, "_ShadowVolumeBuffer", shadowVolumeBuffer);
        silhouetteComputeShader.SetBuffer(kernelHandle, "_ShadowVolumeIndexBuffer", shadowVolumeIndexBuffer);

        if (isSkinnedMesh)
        {
            silhouetteComputeShader.SetBuffer(kernelHandle, "_BoneMatrices", boneMatricesBuffer);
            silhouetteComputeShader.SetBuffer(kernelHandle, "_SkinningBuffer", skinningDataBuffer);
        } else
        {

        }

        // Add these parameters
        silhouetteComputeShader.SetFloat("_MinVertexDistance", 0.001f); // Threshold for considering vertices unique
        silhouetteComputeShader.SetBool("_UseBackfaceCulling", true);  // Enable backface culling
        silhouetteComputeShader.SetFloat("_ShadowBias", 0.005f);       // Small offset to prevent z-fighting



        // Set parameters
        silhouetteComputeShader.SetVector("_LightPosition", light.transform.position);
        silhouetteComputeShader.SetVector("_LightDirection", light.transform.forward);
        silhouetteComputeShader.SetFloat("_LightSpotAngle", light.spotAngle);
        silhouetteComputeShader.SetFloat("_LightRange", light.range);
        silhouetteComputeShader.SetInt("_LightType", light.type == LightType.Point ? 0 : 1);
        silhouetteComputeShader.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
        silhouetteComputeShader.SetInt("_VertexCount", mesh.vertexCount);
        silhouetteComputeShader.SetInt("_IndexCount", mesh.triangles.Length);
        silhouetteComputeShader.SetFloat("_ExtrusionLength", 10f); // Adjust as needed
        silhouetteComputeShader.SetBool("_IsSkinned", isSkinnedMesh);

        // Dispatch
        int threadGroups = Mathf.CeilToInt(mesh.triangles.Length / (float)(threadGroupSize * 3));
        silhouetteComputeShader.Dispatch(kernelHandle, threadGroups, 1, 1);
        computeTime = Time.realtimeSinceStartup - startTime;
    }

    private void RenderShadowVolume()
    {
        // Create a temporary mesh for rendering
        Mesh shadowMesh = new Mesh();
        shadowMesh.indexFormat = IndexFormat.UInt32;

        // Get data from compute buffers
        Vector3[] vertices = new Vector3[mesh.triangles.Length * 4];
        int[] indices = new int[mesh.triangles.Length * 6];
        shadowVolumeBuffer.GetData(vertices);
        shadowVolumeIndexBuffer.GetData(indices);

        // Store for debug visualization
        debugVertices = vertices;
        debugIndices = indices;
        silhouetteEdgeCount = indices.Length / 6;

        // Filter out zero vertices and invalid indices
        List<Vector3> validVertices = new List<Vector3>();
        List<int> validIndices = new List<int>();
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        // Process vertices and build mapping
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i] != Vector3.zero)
            {
                if (!vertexMap.ContainsKey(vertices[i]))
                {
                    vertexMap[vertices[i]] = validVertices.Count;
                    validVertices.Add(vertices[i]);
                }
            }
        }

        // Process indices
        for (int i = 0; i < indices.Length; i += 3)
        {
            bool triangleValid = true;
            int[] remappedIndices = new int[3];

            for (int j = 0; j < 3; j++)
            {
                int index = indices[i + j];
                if (index >= 0 && index < vertices.Length && vertices[index] != Vector3.zero)
                {
                    remappedIndices[j] = vertexMap[vertices[index]];
                }
                else
                {
                    triangleValid = false;
                    break;
                }
            }

            if (triangleValid)
            {
                validIndices.AddRange(remappedIndices);
            }
        }

        // Only proceed if we have valid geometry
        if (validVertices.Count > 0 && validIndices.Count > 0)
        {
            shadowMesh.SetVertices(validVertices);
            shadowMesh.SetIndices(validIndices.ToArray(), MeshTopology.Triangles, 0);
            shadowMesh.RecalculateNormals();
            shadowMesh.RecalculateBounds();

            // Configure shadow material
            if (shadowMaterial != null)
            {
                // Set necessary material properties
                shadowMaterial.SetFloat("_ExtrusionLength", 10f); // Match compute shader value
                shadowMaterial.SetInt("_ZWrite", 1);
                shadowMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Less);
                shadowMaterial.SetFloat("_ShadowIntensity", 0.5f); // Adjust as needed

                // Draw the shadow volume
                Graphics.DrawMesh(shadowMesh, transform.localToWorldMatrix, shadowMaterial, 0);
            }
        }
    }

    private void OnDestroy()
    {
        // Release buffers
        vertexBuffer?.Release();
        indexBuffer?.Release();
        boneMatricesBuffer?.Release();
        skinningDataBuffer?.Release();
        shadowVolumeBuffer?.Release();
        shadowVolumeIndexBuffer?.Release();
    }

    // Structs matching compute shader
    private struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv;
    }

    private struct SkinningData
    {
        public Vector4 boneWeights;
        public Vector4 boneIndices;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || !Application.isPlaying || debugVertices == null) return;
        //Gizmos.matrix = transform.localToWorldMatrix;
        Light nearestLight = FindNearestLight();
        if (nearestLight == null) return;

        // Draw silhouette edges
        if (showSilhouetteEdges)
        {
            Gizmos.color = silhouetteColor;
            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 1 < debugVertices.Length)
                {
                    // Draw base edge
                    DrawThickLine(
                        debugVertices[baseIndex],
                        debugVertices[baseIndex + 1],
                        debugLineWidth,
                        silhouetteColor
                    );

                    // Draw debug sphere at vertices
                    Gizmos.DrawSphere(debugVertices[baseIndex], debugLineWidth * 0.1f);
                    Gizmos.DrawSphere(debugVertices[baseIndex + 1], debugLineWidth * 0.1f);
                }
            }
        }

        // Draw extruded volume with enhanced visualization
        if (showExtrudedVolume)
        {
            Gizmos.color = extrudedVolumeColor;
            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 3 < debugVertices.Length)
                {
                    // Draw quad edges
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

                    // Draw small spheres at extruded vertices
                    Gizmos.DrawSphere(debugVertices[baseIndex + 2], debugLineWidth * 0.1f);
                    Gizmos.DrawSphere(debugVertices[baseIndex + 3], debugLineWidth * 0.1f);
                }
            }
        }

        // Enhanced light connection visualization
        if (showLightConnection)
        {
            Gizmos.color = lightConnectionColor;
            Vector3 lightPos = nearestLight.transform.position;

            // Draw light position
            Gizmos.DrawWireSphere(lightPos, debugLineWidth * 0.5f);

            for (int i = 0; i < silhouetteEdgeCount; i++)
            {
                int baseIndex = i * 4;
                if (baseIndex + 1 < debugVertices.Length)
                {
                    // Draw lines from light to silhouette vertices
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


    private void DrawThickLine(Vector3 start, Vector3 end, float width, Color color)
    {
        #if UNITY_EDITOR
                // Calculate the camera's right vector
                Vector3 cameraRight = SceneView.currentDrawingSceneView?.camera?.transform.right ?? Vector3.right;

                // Calculate vertices for the thick line
                Vector3 forward = (end - start).normalized;
                Vector3 right = Vector3.Cross(forward, cameraRight).normalized * width * 0.5f;

                Vector3[] vertices = new Vector3[4];
                vertices[0] = start - right;
                vertices[1] = start + right;
                vertices[2] = end - right;
                vertices[3] = end + right;

                // Draw the thick line using two triangles
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