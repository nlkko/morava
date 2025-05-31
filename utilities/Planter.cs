using Godot;
using System;
using System.Collections.Generic;

// TODO:
// Add support for multiple meshes, etc: flowers
public partial class Planter : MeshInstance3D
{
    private struct SurfacePoint
    {
        public Vector3 Position;
        public Vector3 Normal;
    }

    [Export]
    Mesh instantiatedMesh;

    [Export]
    int instanceCount = 1000; // Amount of instatianted meshes to place

    [Export]
    float scale = 1.0f; // Random scale variation

    [Export]
    float scaleVariation = 0.2f; // Random scale variation

    [Export]
    public ShaderMaterial instantiatedShaderMaterial; // The shader material used for the instantiated mesh as material override


    private MultiMesh _multiMesh;
    private MultiMeshInstance3D _instance;
    private Random _random = new Random();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Populate();
    }

    private void Populate()
    {
        // Check if mesh is assigned
        if (instantiatedMesh == null)
        {
            GD.PrintErr("No mesh assigned to Planter!");
            return;
        }

        _instance = new MultiMeshInstance3D();
        // Disable casting shadows from instances
        _instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(_instance);

        // Create MultiMesh and assign it directly to this MultiMeshInstance3D
        _multiMesh = new MultiMesh();
        _multiMesh.Mesh = instantiatedMesh;

        // Set transform format BEFORE setting instance count
        _multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        _multiMesh.InstanceCount = instanceCount;

        // Assign the MultiMesh to this MultiMeshInstance3D
        _instance.Multimesh = _multiMesh;
        
        // If shader material is proved add to multimesh
        if (instantiatedShaderMaterial != null) _instance.MaterialOverride = instantiatedShaderMaterial;

        // Get random surface points and set transforms
        var surfacePoints = GetRandomSurfacePoints(instanceCount);

        // Set the transform of the instances
        for (int i = 0; i < _multiMesh.InstanceCount; i++)
        {
            var surfacePoint = surfacePoints[i];
            var transform = CreateTransformAtSurfacePoint(surfacePoint);
            _multiMesh.SetInstanceTransform(i, transform);
        }
    }

    private List<SurfacePoint> GetRandomSurfacePoints(int count)
    {
        var points = new List<SurfacePoint>();
        var meshDataTool = new MeshDataTool();
        if (Mesh == null)
        {
            GD.PrintErr("No mesh assigned to MeshInstance3D!");
            return points;
        }

        ArrayMesh arrayMesh;

        if (Mesh is ArrayMesh)
        {
            arrayMesh = (ArrayMesh) Mesh;
        }
        else
        {
            // Convert to ArrayMesh
            arrayMesh = new ArrayMesh();
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Mesh.SurfaceGetArrays(0));
        }

        meshDataTool.CreateFromSurface(arrayMesh, 0);

        int faceCount = meshDataTool.GetFaceCount();
        if (faceCount == 0)
        {
            GD.PrintErr("MeshInstance3D Mesh has no faces!");
            return points;
        }

        var triangleAreas = new float[faceCount];
        float totalArea = 0f;

        for (int i = 0; i < faceCount; i++)
        {
            var v1 = meshDataTool.GetVertex(meshDataTool.GetFaceVertex(i, 0));
            var v2 = meshDataTool.GetVertex(meshDataTool.GetFaceVertex(i, 1));
            var v3 = meshDataTool.GetVertex(meshDataTool.GetFaceVertex(i, 2));

            // Calculate triangle area using cross product
            var area = (v2 - v1).Cross(v3 - v1).Length() * 0.5f;
            triangleAreas[i] = area;
            totalArea += area;
        }

        // Generate random points
        for (int i = 0; i < count; i++)
        {
            // Select random triangle weighted by area
            int triangleIndex = SelectWeightedRandomTriangle(triangleAreas, totalArea);

            // Get random point on selected triangle
            var surfacePoint = GetRandomPointOnTriangle(meshDataTool, triangleIndex);
            points.Add(surfacePoint);
        }

        return points;
    }

    private SurfacePoint GetRandomPointOnTriangle(MeshDataTool meshData, int triangleIndex)
    {
        // Get triangle vertices
        var v1 = meshData.GetVertex(meshData.GetFaceVertex(triangleIndex, 0));
        var v2 = meshData.GetVertex(meshData.GetFaceVertex(triangleIndex, 1));
        var v3 = meshData.GetVertex(meshData.GetFaceVertex(triangleIndex, 2));

        // Get triangle normals
        var n1 = meshData.GetVertexNormal(meshData.GetFaceVertex(triangleIndex, 0));
        var n2 = meshData.GetVertexNormal(meshData.GetFaceVertex(triangleIndex, 1));
        var n3 = meshData.GetVertexNormal(meshData.GetFaceVertex(triangleIndex, 2));

        // Generate random barycentric coordinates
        float r1 = (float)_random.NextDouble();
        float r2 = (float)_random.NextDouble();

        // Ensure point is inside triangle
        if (r1 + r2 > 1.0f)
        {
            r1 = 1.0f - r1;
            r2 = 1.0f - r2;
        }

        float r3 = 1.0f - r1 - r2;

        // Interpolate position and normal
        var position = v1 * r1 + v2 * r2 + v3 * r3;
        var normal = (n1 * r1 + n2 * r2 + n3 * r3).Normalized();

        return new SurfacePoint { Position = position, Normal = normal };
    }

    private int SelectWeightedRandomTriangle(float[] areas, float totalArea)
    {
        float randomValue = (float)_random.NextDouble() * totalArea;
        float currentSum = 0f;

        for (int i = 0; i < areas.Length; i++)
        {
            currentSum += areas[i];
            if (randomValue <= currentSum)
                return i;
        }

        return areas.Length - 1; // Fallback
    }

    private Transform3D CreateTransformAtSurfacePoint(SurfacePoint surfacePoint)
    {
        var position = surfacePoint.Position;
        var normal = surfacePoint.Normal;

        // Correct for billboard
        Basis basis = new Basis(-normal, -Vector3.Down, -Vector3.Back.Cross(normal).Normalized());

        // Apply base scale with random variation
        float randomScale = scale + ((float)_random.NextDouble() - 0.5f) * 2.0f * scaleVariation;
        basis = basis.Scaled(Vector3.One * randomScale);

        // Add random rotation around the forward axis (±15 degrees for 30 degree total range)
        float randomRotation = ((float)_random.NextDouble() - 0.5f) * Mathf.DegToRad(30f);
        basis = basis.Rotated(normal, randomRotation);

        return new Transform3D(basis, position);
    }
}