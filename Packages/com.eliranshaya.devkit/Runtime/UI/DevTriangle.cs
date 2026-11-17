#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// A solid triangle pointing down, filling its rect: the arrow on a dropdown cell.
    /// <para>
    /// Drawn as three vertices rather than an arrow glyph or a sprite. A glyph depends on the
    /// built-in font covering it, and a missing one renders as an empty box; a sprite would be an
    /// asset. Geometry is crisp at every scale and needs neither.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <see cref="RequireComponent"/> is not optional. uGUI 2.0 (Unity 6) moved it off
    /// <see cref="Graphic"/> onto <see cref="Image"/>, <see cref="RawImage"/> and <see cref="Text"/>,
    /// so a Graphic subclass no longer gets a <see cref="CanvasRenderer"/> on its own - and in the
    /// Editor the first clipping pass then throws a MissingComponentException.
    /// </remarks>
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class DevTriangle : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(rect.xMin, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(rect.center.x, rect.yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
        }
    }
}
#endif
