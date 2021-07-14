using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour {
	private Transform target;
    private Transform seeker;
    Grid grid;
    public List<Node> path=new List<Node>();
    private int ownColliderRadius;
    private bool lookingToGo=false;
    public Vector3[] worldPath;
    public int pathIndex = 0;

    void Awake() {
		grid = GameObject.FindGameObjectWithTag("mocap").GetComponent<Grid> ();
        ownColliderRadius = Mathf.RoundToInt(Mathf.Sqrt(2) * gameObject.GetComponent<Collider>().bounds.size.x / grid.getNodeDiameter())/2;
        seeker = gameObject.transform;
    }

	void Update() 
	{
	}

	void FindPath(Vector3 startPos, Vector3 targetPos) {
		Node startNode = grid.NodeFromWorldPoint(startPos);
		Node targetNode = grid.NodeFromWorldPoint(targetPos);

		List<Node> openSet = new List<Node>();
		HashSet<Node> closedSet = new HashSet<Node>();
		openSet.Add(startNode);

		while (openSet.Count > 0) {
			Node node = openSet[0];
			for (int i = 1; i < openSet.Count; i ++) {
				if (openSet[i].fCost < node.fCost || openSet[i].fCost == node.fCost) {
					if (openSet[i].hCost < node.hCost)
						node = openSet[i];
				}
			}

			openSet.Remove(node);
			closedSet.Add(node);

			if (node == targetNode) {
				worldPath=RetracePath(startNode,targetNode);
				return;
			}

			foreach (Node neighbour in grid.GetNeighbours(node)) {
				if (!neighbour.walkable && neighbour.distance(startNode)<ownColliderRadius || closedSet.Contains(neighbour)) {
					continue;
				}

				int newCostToNeighbour = node.gCost + GetDistance(node, neighbour);
				if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
					neighbour.gCost = newCostToNeighbour;
					neighbour.hCost = GetDistance(neighbour, targetNode);
					neighbour.parent = node;

					if (!openSet.Contains(neighbour))
						openSet.Add(neighbour);
				}
			}
		}
	}

	Vector3[] RetracePath(Node startNode, Node endNode) {
        path.Clear();
        Node currentNode = endNode;

		while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
        Vector3[] waypoints = SimplifyPath();
        Array.Reverse(waypoints);
        return waypoints;

    }

    Vector3[] SimplifyPath()
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }
    int GetDistance(Node nodeA, Node nodeB) {
		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

		if (dstX > dstY)
			return 14*dstY + 10* (dstX-dstY);
		return 14*dstX + 10 * (dstY-dstX);
	}

	public void setTarget(Transform targetTransform)
	{
        target = targetTransform;
        lookingToGo = true;
    }

	public bool getLookingToGo()
	{
        return lookingToGo;
    }
	public void Stop()
	{
        lookingToGo = false;
    }

	public void AstarPathing()
	{
        FindPath (seeker.position, target.position);
    }
}
