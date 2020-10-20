using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shapes
{
    public static Vector3[] verticalLine = new Vector3[] {
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0)
    };

    public static Vector3[] horizontalLine = new Vector3[] {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0)
    };

    public static Vector3[] diagonalLeft = new Vector3[]
    {
        new Vector3(-1,1),
        new Vector3(1,-1),
    };

    public static Vector3[] diagonalRight = new Vector3[]
    {
        new Vector3(1,1),
        new Vector3(-1,-1)
    };

    public static Vector3[] lightningBolt = new Vector3[]
    {
        new Vector3(0.5f,1),
        new Vector3(-1,0),
        new Vector3(1,0),
        new Vector3(0,-1)
    };
}
