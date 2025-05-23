using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid3D<T>
{
    public T[] data;

    public List<T> values;

    public Vector3Int Size
    {
        get; private set;
    }
    public Vector3Int Offset
    {
        get; set;
    }
    public int DataLen
    {
        get; private set;
    }

    public Grid3D(Vector3Int size, Vector3Int offset)
    {
        values = new List<T>();
        Size = size;// + Vector3Int.one;
        Offset = offset;
        DataLen = Size.x * Size.y * Size.z;
        data = new T[DataLen];
    }

    public int GetIndex(Vector3Int pos)
    {
        int result = pos.x + (Size.x * pos.y) + (Size.x * Size.y * pos.z);
        GrimmGen.PrintDebug(result);
        return result;
    }

    public bool InBounds(Vector3Int pos)
    {
        Vector3Int v3One = new(1, 0, 1);
        return new BoundsInt(Vector3Int.zero, Size - v3One).Contains(pos + Offset);
    }

    public T this[int x, int y, int z]
    {
        get => this[new Vector3Int(x, y, z)];
        set => this[new Vector3Int(x, y, z)] = value;
    }

    public T this[Vector3Int pos]
    {
        get
        {
            pos += Offset;
            int index = GetIndex(pos);
            return data[index];
        }
        set
        {
            pos += Offset;
            data[GetIndex(pos)] = value;
            if (value != null)
            {
                values.Add(value);
            }
        }
    }
}
