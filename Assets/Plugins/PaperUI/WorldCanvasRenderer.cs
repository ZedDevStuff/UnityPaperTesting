using System.Collections.Generic;
using Prowl.Quill;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class WorldCanvasRenderer : MonoBehaviour, ICanvasMeshHandler
{
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }
    private List<Vector3> _vertices = new(1024);
    private List<Vector2> _uvs = new(1024);
    private List<int> _indices = new(1024);
    private List<Color32> _colors = new(1024);

    public void Clear()
    {
        _vertices.Clear();
        _uvs.Clear();
        _indices.Clear();
        _colors.Clear();
    }

    public void AddMeshData(Vertex a, Vertex b, Vertex c)
    {
        _vertices.Add(new Vector3(a.x, a.y, 0));
        _vertices.Add(new Vector3(b.x, b.y, 0));
        _vertices.Add(new Vector3(c.x, c.y, 0));

        _uvs.Add(new Vector2(a.u, a.v));
        _uvs.Add(new Vector2(b.u, b.v));
        _uvs.Add(new Vector2(c.u, c.v));

        _colors.Add(new Color32(a.r, a.g, a.b, a.a));
        _colors.Add(new Color32(b.r, b.g, b.b, b.a));
        _colors.Add(new Color32(c.r, c.g, c.b, c.a));

        int baseIndex = _vertices.Count - 3;
        _indices.Add(baseIndex);
        _indices.Add(baseIndex + 1);
        _indices.Add(baseIndex + 2);
    }

    public void BuildMesh(Material material)
    {
        var mesh = new Mesh();
        mesh.SetVertices(_vertices);
        mesh.SetUVs(0, _uvs);
        mesh.SetColors(_colors);
        mesh.SetTriangles(_indices, 0);
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
    }
}
