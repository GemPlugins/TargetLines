using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace TargetLines;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct UIRect
{
    [FieldOffset(0x00)] public Vector2 Position;
    [FieldOffset(0x08)] public Vector2 Size;
    [FieldOffset(0x00)] public Vector2 TopLeft; // identical to pos
    [FieldOffset(0x10)] public Vector2 TopRight;
    [FieldOffset(0x18)] public Vector2 BottomLeft;
    [FieldOffset(0x20)] public Vector2 BottomRight;

    public float Left
    {
        get
        {
            return TopLeft.X;
        }
        set
        {
            TopLeft.X = value;
            BottomLeft.X = value;
        }
    }

    public float Right
    {
        get
        {
            return TopRight.X;
        }
        set
        {
            TopRight.X = value;
            BottomRight.X = value;
        }
    }

    public float Top
    {
        get
        {
            return TopLeft.Y;
        }
        set
        {
            TopLeft.Y = value;
            TopRight.Y = value;
        }
    }

    public float Bottom
    {
        get
        {
            return BottomLeft.Y;
        }
        set
        {
            BottomLeft.Y = value;
            BottomRight.Y = value;
        }
    }

    public float Width => Size.X;
    public float Height => Size.Y;
    public float Area => Width * Height;

    public UIRect(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        Initialize();
    }


    public void Initialize()
    {
        Left = TopLeft.X;
        Right = TopLeft.X + Size.X;
        Top = TopLeft.Y;
        Bottom = TopLeft.Y + Size.Y;
    }

    public bool Contains(UIRect other)
    {
        return Left <= other.Left && Right >= other.Right &&
               Top <= other.Top && Bottom >= other.Bottom;
    }

    public bool Intersects(UIRect other)
    {
        return Left <= other.Right && Right >= other.Left &&
               Top <= other.Bottom && Bottom >= other.Top;
    }

    public UIRect Union(UIRect other)
    {
        float l = Math.Min(Left, other.Left);
        float t = Math.Min(Top, other.Top);
        float r = Math.Max(Right, other.Right);
        float b = Math.Max(Bottom, other.Bottom);

        return new UIRect(
            new Vector2(l, t),
            new Vector2(r - l, b - t)
        );
    }
}


