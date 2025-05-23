using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Vector2 are world cooridinates, they can either be whole numbers or half numbers
 * Vector2Int are grid coordinates, these are only whole numbers
 * Be sure that when passing values to a Grid2D object the correct type is used.
 */

public class Grid2D<T>
{
    public T[] data;
    public Vector2[] v2;
    //public Dictionary<float, int> graphConversion { get; set; }
    public List<float> graphConversion
    {
        get; set;
    }

    public Vector2 Size
    {
        get; private set;
    }
    public Vector2Int GraphSize
    {
        get; private set;
    }
    public Vector2 Offset
    {
        get; set;
    }
    public Vector2Int GraphOffset
    {
        get; set;
    }

    public Grid2D(Vector2 size, Vector2 offset)
    {
        Size = size;
        GraphSize = new Vector2Int((int)size.x * 2, (int)size.y * 2);
        Offset = offset;
        GraphOffset = new Vector2Int((int)offset.x * 2, (int)offset.y * 2);

        data = new T[GraphSize.x * GraphSize.y];
        v2 = new Vector2[GraphSize.x * GraphSize.y];
        graphConversion = GenerateConversions(GraphSize);
    }

    //private Dictionary<float, int> GenerateConversions(Vector2Int size)
    private List<float> GenerateConversions(Vector2Int size)
    {
        //Dictionary<float, int> result = new Dictionary<float, int>();
        List<float> result = new();
        float key = 0;
        int limit = 0;
        if (size.x > size.y)
        {
            limit = size.x;
        }
        else
        {
            limit = size.y;
        }

        for (int i = 0; i <= limit; i++)
        {
            result.Insert(i, key);
            key += .5f;
        }

        return result;
    }

    public Vector2 GetVector2(int index) => v2[index];

    public Vector2[] GetAllVector2() => v2;

    public int GetIndex(Vector2 pos)
    {
        Vector2Int intPos = WorldPosToGraphPos(pos);
        //This limits the size of the map by one unit but is useful
        int index = intPos.x + (GraphSize.x * intPos.y);
        if (index > (GraphSize.x * GraphSize.y))
        {
            GrimmGen.PrintDebug("Weird shit is happening");
        }

        return index;
    }

    public int GetIndex(Vector2Int pos)
        //Vector2Int intPos = WorldPosToGraphPos(pos);
        //This limits the size of the map by one unit but is useful
        => pos.x + (GraphSize.x * pos.y);

    public Vector2Int WorldPosToGraphPos(Vector2 pos)
    {
        float x = pos.x;
        float y = pos.y;
        //if the graph unit ever changes this will probably need updated
        if (pos.x * 10.0f % 2 == 1)
        {
            x = System.MathF.Round(pos.x, 1, MidpointRounding.AwayFromZero);
        }

        if (pos.y * 10.0f % 2 == 1)
        {
            y = System.MathF.Round(pos.y, 1, MidpointRounding.AwayFromZero);
        }

        if (graphConversion.Contains(x) && graphConversion.Contains(y))
        {
            return new Vector2Int(graphConversion.IndexOf(x), graphConversion.IndexOf(y));
        }
        else
        {
            GrimmGen.PrintDebug($"Position {pos} not found in Grid2D");
            return Vector2Int.zero;
        }
    }

    public bool InBounds(Vector2 pos) => new Rect(Vector2Int.zero, Size - new Vector2(1, 1)).Contains(pos + GraphOffset);

    public bool InBounds(Vector2Int pos) => new RectInt(Vector2Int.zero, GraphSize - new Vector2Int(1, 1)).Contains(pos + GraphOffset);

    public T this[float x, float y]
    {
        get => this[new Vector2(x, y)];
        set => this[new Vector2(x, y)] = value;
    }

    public T this[int x, int y]
    {
        get => this[new Vector2Int(x, y)];
        set => this[new Vector2Int(x, y)] = value;
    }

    public T this[Vector2 pos]
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
            int index = GetIndex(pos);
            v2[index] = pos;
            data[index] = value;
        }
    }

    public T this[Vector2Int pos]
    {
        get
        {
            pos += GraphOffset;
            int index = GetIndex(pos);
            return data[index];
        }
        set
        {
            pos += GraphOffset;
            int index = GetIndex(pos);
            data[index] = value;
        }
    }
}
