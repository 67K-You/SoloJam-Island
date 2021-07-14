using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{

    public LayerMask unwalkableMask;
    private Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Start()
    {
        nodeDiameter = nodeRadius * 2;
        Vector3 floorSize = GameObject.FindGameObjectWithTag("Floor").GetComponent<Collider>().bounds.size;
        gridWorldSize = new Vector2(floorSize.x, floorSize.z);
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    void Update()
    {
        StartCoroutine(GridCheck());
    }



    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                grid[x, y] = new Node(walkable, worldPoint,x,y);
            }
        }

    }

    public List<Node> GetNeighbours(Node node)
    {
		List<Node> neighbours = new List<Node>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0)
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {

        float percentX = worldPosition.x / gridWorldSize.x + 0.5f;
        float percentY = worldPosition.z / gridWorldSize.y + 0.5f;

        int x = Mathf.FloorToInt(Mathf.Min(gridSizeX * percentX, gridSizeX-1));
        int y = Mathf.FloorToInt(Mathf.Min(gridSizeY * percentY, gridSizeY-1));
        return grid[x, y];
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));


        if (grid != null)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter/2.0f));
            }
        }
    }

    public float getNodeDiameter()
    {
        return nodeDiameter;
    }

    IEnumerator GridCheck()
    {
        CreateGrid();
        yield return new WaitForSeconds(0.05f);
    }
}

