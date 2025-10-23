using Prowl.Quill;
using UnityEngine;

public interface ICanvasMeshHandler
{
    public void Clear();
    public void AddMeshData(Vertex a, Vertex b, Vertex c);
    public void BuildMesh(Material material);
}
