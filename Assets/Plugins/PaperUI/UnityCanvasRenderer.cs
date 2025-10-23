using System;
using System.Collections.Generic;
using Prowl.Quill;
using Prowl.Vector;
using UnityEngine;

public class UnityCanvasRenderer : ICanvasRenderer
{
    private readonly ICanvasMeshHandler _meshHandler;
    public Material Material;
    public static Texture2D DefaultTex { get; private set; }

    public Texture2D MainTex
    {
        set => Material.SetTexture("_MainTex", value);
    }
    public Matrix4x4 ScissorMatrix
    {
        set => Material.SetMatrix("_ScissorMatrix", value);
    }
    public Vector2 ScissorExtents
    {
        set => Material.SetVector("_ScissorExtents", value);
    }
    public Matrix4x4 BrushMatrix
    {
        set => Material.SetMatrix("_BrushMatrix", value);
    }
    public int BrushType
    {
        set => Material.SetInt("_BrushType", value);
    }
    public UnityEngine.Color BrushColor1
    {
        set => Material.SetColor("_BrushColor1", value);
    }
    public UnityEngine.Color BrushColor2
    {
        set => Material.SetColor("_BrushColor2", value);
    }
    public Vector4 BrushParams
    {
        set => Material.SetVector("_BrushParams", value);
    }
    public Vector4 BrushParams2
    {
        set => Material.SetVector("_BrushParams2", value);
    }

    public UnityCanvasRenderer(ICanvasMeshHandler meshHandler)
    {
        Material = new Material(Shader.Find("PaperUI/Shader"));
        _meshHandler = meshHandler;
        if (DefaultTex == null)
        {
            DefaultTex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            DefaultTex.SetPixel(0, 0, new UnityEngine.Color(1f, 1f, 1f, 1f));
            DefaultTex.Apply();
        }
    }

    public object CreateTexture(uint width, uint height)
    {
        return new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Int2 GetTextureSize(object texture)
    {
        if (texture is not Texture2D tex)
            throw new ArgumentException("Texture must be of type Texture2D", nameof(texture));
        return new Int2(tex.width, tex.height);
    }

    public void RenderCalls(Prowl.Quill.Canvas canvas, IReadOnlyList<DrawCall> drawCalls)
    {
        _meshHandler.Clear();
        int index = 0;
        foreach (var drawCall in drawCalls)
        {
            SetValues(drawCall);

            for (int i = 0; i < drawCall.ElementCount; i += 3)
            {
                var a = canvas.Vertices[(int)canvas.Indices[index]];
                var b = canvas.Vertices[(int)canvas.Indices[index + 1]];
                var c = canvas.Vertices[(int)canvas.Indices[index + 2]];
                Material.SetPass(0);
                _meshHandler.AddMeshData(a, b, c);
                index += 3;
            }
        }
        _meshHandler.BuildMesh(Material);
    }

    public void SetTextureData(object texture, Prowl.Vector.Geometry.IntRect bounds, byte[] data)
    {
        if (texture is not Texture2D tex)
            throw new ArgumentException("Texture must be of type Texture2D", nameof(texture));
        tex.Reinitialize(bounds.Size.X, bounds.Size.Y);
        tex.LoadRawTextureData(data);
        tex.Apply();
    }

    private void SetValues(DrawCall drawCall)
    {
        // Bind the texture if available, otherwise use default
        Texture2D textureToUse = DefaultTex;
        if (drawCall.Texture != null)
            textureToUse = (Texture2D)drawCall.Texture;

        MainTex = textureToUse;

        // Set scissor rectangle
        drawCall.GetScissor(out var scissor, out var extent);

        ScissorMatrix = Matrix4x4.Transpose(ToMatrix4x4(scissor));
        ScissorExtents = new Vector2((float)extent.X, (float)extent.Y);

        // Set gradient parameters
        BrushType = (int)drawCall.Brush.Type;
        if (drawCall.Brush.Type != Prowl.Quill.BrushType.None)
        {
            BrushMatrix = Matrix4x4.Transpose(ToMatrix4x4(drawCall.Brush.BrushMatrix));
            BrushColor1 = ToUnityColor(drawCall.Brush.Color1);
            BrushColor2 = ToUnityColor(drawCall.Brush.Color2);
            BrushParams = new Vector4((float)drawCall.Brush.Point1.X, (float)drawCall.Brush.Point1.Y, (float)drawCall.Brush.Point2.X, (float)drawCall.Brush.Point2.Y);
            BrushParams2 = new Vector4((float)drawCall.Brush.CornerRadii, (float)drawCall.Brush.Feather, 0f, 0f);
        }
    }

    private static Matrix4x4 ToMatrix4x4(Double4x4 mat)
    {
        return new Matrix4x4
        (
            new Vector4((float)mat.c0.X, (float)mat.c0.Y, (float)mat.c0.Z, (float)mat.c0.W),
            new Vector4((float)mat.c1.X, (float)mat.c1.Y, (float)mat.c1.Z, (float)mat.c1.W),
            new Vector4((float)mat.c2.X, (float)mat.c2.Y, (float)mat.c2.Z, (float)mat.c2.W),
            new Vector4((float)mat.c3.X, (float)mat.c3.Y, (float)mat.c3.Z, (float)mat.c3.W)
        );
    }
    private static UnityEngine.Color ToUnityColor(Prowl.Vector.Color32 color)
    {
        return new UnityEngine.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }
}

public class MeshGenerator
{
    public static Mesh CreateTriangle(Vertex v1, Vertex v2, Vertex v3)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[3]
        {
            new(v1.x, v1.y, 0),
            new(v2.x, v2.y, 0),
            new(v3.x, v3.y, 0)
        };

        Vector2[] uv = new Vector2[3]
        {
            new(v1.u, v1.v),
            new(v2.u, v2.v),
            new(v3.u, v3.v)
        };

        UnityEngine.Color32[] colors = new UnityEngine.Color32[3]
        {
            new(v1.r, v1.g, v1.b, v1.a),
            new(v2.r, v2.g, v2.b, v2.a),
            new(v3.r, v3.g, v3.b, v3.a)
        };

        int[] triangles = new int[3] { 0, 1, 2 };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors32 = colors;
        mesh.triangles = triangles;
        return mesh;
    }
}
