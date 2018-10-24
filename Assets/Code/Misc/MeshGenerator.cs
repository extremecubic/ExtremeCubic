using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
	public static Mesh Create2DGrid(int sizeX, int sizeY, float cellSize)
	{
		Mesh mesh = new Mesh();

		float half = cellSize * 0.5f;		

		// create vertex and index array
		Vector3[] vertices = new Vector3[(sizeX * sizeY) * 4];
		int[] indices = new int[(sizeX * sizeY) * 6];

		int row = 0;
		int tileCount = 0;
		int indexVertex = 0;
		int indexIndice = 0;

		// loop over and set all vertices and indices
		for (int i = 0; i < sizeX * sizeY; i++)
		{
			vertices[indexVertex + 0] = new Vector3(tileCount - half, -half, row + half); // top left
			vertices[indexVertex + 1] = new Vector3(tileCount + half, -half, row + half); // top right
			vertices[indexVertex + 2] = new Vector3(tileCount - half, -half, row - half); // bottom left
			vertices[indexVertex + 3] = new Vector3(tileCount + half, -half, row - half); // bottom right

			indices[indexIndice + 0] = indexVertex;
			indices[indexIndice + 1] = indexVertex + 1;
			indices[indexIndice + 2] = indexVertex + 2;
			indices[indexIndice + 3] = indexVertex + 2;
			indices[indexIndice + 4] = indexVertex + 1;
			indices[indexIndice + 5] = indexVertex + 3;

			tileCount++;
			indexVertex += 4;
			indexIndice += 6;

			if (tileCount == sizeX)
			{
				row++;
				tileCount = 0;
			}
		}

		// assign vertices and indices
		mesh.vertices = vertices;
		mesh.triangles = indices;

		return mesh;
	}	
}
