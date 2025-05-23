using System;
using System.Collections.Generic;
using UnityEngine;
using BlueRaja;

public class DungeonPathfinder2D
{
    public class Node
    {
        public Vector2Int Position
        {
            get; private set;
        }
        public Node Previous
        {
            get; set;
        }
        public (double, double) Cost
        {
            get; set;
        }

        public Node(Vector2Int position) => Position = position;
    }

    public struct PathCost
    {
        public bool traversable;
        //Adjusted for addition of turn count
        public (double, double) cost;
    }

    private static readonly Vector2Int[] pfNeighbors = {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    private Vector2Int[] neighbors = new Vector2Int[pfNeighbors.Length];

    Grid2D<Node> grid;
    SimplePriorityQueue<Node, float> queue;
    HashSet<Node> closed;
    Stack<Vector2Int> stack;
    int unitSize;

    public DungeonPathfinder2D(Vector2Int size, int unitSize)
    {
        this.unitSize = unitSize;
        Vector2 offset = Vector2.zero;
        grid = new Grid2D<Node>(size, offset);

        for (int i = 0; i < neighbors.Length; i++)
        {
            neighbors[i] = pfNeighbors[i] * unitSize;
        }

        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();
        stack = new Stack<Vector2Int>();


        for (int x = 0; x < grid.GraphSize.x; x += unitSize)
        {
            for (int y = 0; y < grid.GraphSize.y; y += unitSize)
            {
                grid[x, y] = new Node(new Vector2Int(x, y));
            }
        }
    }

    void ResetNodes()
    {
        var size = grid.GraphSize;

        for (int x = 0; x < size.x; x += unitSize)
        {
            for (int y = 0; y < size.y; y += unitSize)
            {
                var node = grid[x, y];
                node.Previous = null;
                //Adusted for turn penalty
                node.Cost = (float.PositiveInfinity, float.PositiveInfinity);
            }
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int unitSize, Func<Node, Node, PathCost> costFunction)
    {
        ResetNodes();
        queue.Clear();
        closed.Clear();

        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();

        //Adusted for turn penalty
        grid[start].Cost = (0, 0);
        queue.Enqueue(grid[start], 0);

        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            closed.Add(node);

            if (node.Position == grid[end].Position)
            {
                return ReconstructPath(node);
            }

            List<(Node, (double, double))> neighborNodes = new();
            (Node, (double, double)) bestNeighbor = (null, (0, 0));
            (Node, (double, double)) thisNeighbor = (null, (0, 0));
            foreach (var offset in neighbors)
            {
                //Calculate cost of each neighbor
                if (!grid.InBounds(node.Position + offset)) continue;
                var neighbor = grid[node.Position + offset];
                if (closed.Contains(neighbor)) continue; //if neighbor has already been traveled in this path

                var pathCost = costFunction(node, neighbor); //get the path cost of the neighbor
                if (!pathCost.traversable) continue;

                //newCost is the sum of the cost of the neighbor and the cost of the current node?
                //Adds the new cost to the cost of the path.
                //Item1 is the distance and edge traversal cost
                //Item2 is the accumulated turn cost of the path.
                (double, double) newCost = (node.Cost.Item1 + pathCost.cost.Item1, node.Cost.Item2 + pathCost.cost.Item2);
                //neighborNodes.Add((neighbor, newCost));
                thisNeighbor = (neighbor, newCost);
                if (bestNeighbor.Item1 == null)
                {
                    bestNeighbor = thisNeighbor;
                    continue;
                }
                //Keep the neighbor with the best distance and number of turns. Add it to the queue
                if (thisNeighbor.Item2.Item1 == bestNeighbor.Item2.Item1)
                {
                    if (thisNeighbor.Item2.Item2 != bestNeighbor.Item2.Item2)
                    {
                        if (thisNeighbor.Item2.Item2 < bestNeighbor.Item2.Item2)
                        {
                            bestNeighbor = thisNeighbor;
                        }
                    }
                }
                else
                {
                    if (thisNeighbor.Item2.Item1 < bestNeighbor.Item2.Item1)
                    {
                        bestNeighbor = thisNeighbor;
                    }
                }
            }

            if (bestNeighbor.Item1 != null)
            {
                bestNeighbor.Item1.Previous = node;
                bestNeighbor.Item1.Cost = bestNeighbor.Item2;

                //Places the node according to it's cost. lower cost are higher so we always grab the lowest cost neighbor
                if (queue.TryGetPriority(node, out float existingPriority))
                {
                    queue.UpdatePriority(node, (float)bestNeighbor.Item2.Item1);
                }
                else
                {
                    queue.Enqueue(bestNeighbor.Item1, (float)bestNeighbor.Item1.Cost.Item1);
                }
            }
        }

        return null;
    }

    List<Vector2Int> ReconstructPath(Node node)
    {
        List<Vector2Int> result = new();

        while (node != null)
        {
            stack.Push(node.Position);
            node = node.Previous;
        }

        while (stack.Count > 0)
        {
            result.Add(stack.Pop());
        }

        return result;
    }
}
