using UnityEngine;
using UnityEngine.UI;

namespace VisualImpairmentSupport;

internal sealed class SlotBorderGraphic : MaskableGraphic
{
    private const float BorderThickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float thickness = Mathf.Min(BorderThickness, Mathf.Min(rect.width, rect.height) / 2f);
        UIVertex[] quad = new UIVertex[4];

        AddQuad(vertexHelper, quad, rect.xMin, rect.yMax - thickness, rect.xMax, rect.yMax);
        AddQuad(vertexHelper, quad, rect.xMin, rect.yMin, rect.xMax, rect.yMin + thickness);
        AddQuad(vertexHelper, quad, rect.xMin, rect.yMin + thickness, rect.xMin + thickness, rect.yMax - thickness);
        AddQuad(vertexHelper, quad, rect.xMax - thickness, rect.yMin + thickness, rect.xMax, rect.yMax - thickness);
    }

    private void AddQuad(VertexHelper vertexHelper, UIVertex[] quad, float xMin, float yMin, float xMax, float yMax)
    {
        Color32 vertexColor = color;
        quad[0] = CreateVertex(xMin, yMin, vertexColor);
        quad[1] = CreateVertex(xMin, yMax, vertexColor);
        quad[2] = CreateVertex(xMax, yMax, vertexColor);
        quad[3] = CreateVertex(xMax, yMin, vertexColor);
        vertexHelper.AddUIVertexQuad(quad);
    }

    private static UIVertex CreateVertex(float x, float y, Color32 vertexColor)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = new Vector3(x, y, 0f);
        vertex.color = vertexColor;
        return vertex;
    }
}