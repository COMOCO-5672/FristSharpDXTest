using SlimDX;
using System.Runtime.InteropServices;

namespace RenderCore.DataStruct
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VERTEX
    {
        public Vector3 pos;        // vertex untransformed position
        public uint color;         // diffuse color
        public Vector2 texPos;     // texture relative coordinates
    };
}