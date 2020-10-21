using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using UnityEditor.iOS.Extensions.Common;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class LineDraw : MonoBehaviour
{
    public Camera cam;

    [Range(0.01f, 1)]
    public float linePointSpacing = 0.25f;
    public float detectionLeniency = 0.3f;

    private int currPoints;

    private LineRenderer lr;

    private Vector3[][] shapes;

    private List<Vector3> linePoints = new List<Vector3>();

    private List<Vector3> gizmoPoints = new List<Vector3>();
    private List<Vector3> insidePointsList = new List<Vector3>();
    private List<Vector3> shapePointGiz = new List<Vector3>();
    private List<Vector3> outsidePoints = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;

        shapes = new Vector3[][] { 
            Shapes.verticalLine, //0
            Shapes.horizontalLine, //1
            Shapes.diagonalLeft, //2
            Shapes.diagonalRight, //3
            Shapes.N, //4
            Shapes.lightningBolt, //5
            Shapes.closedXLeft, //6
            Shapes.square //7
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            //CONVERT MOUSE POS TO WORLD POS
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            //SEE IF CURRENT POINT COUNT IS GREATER THAN 2 (NEEDED FOR DIST CALC)
            if (lr.positionCount > 2)
            {
                //GET DIST BETWEEN PREVIOUS LINE POINT AND MOUSEPOS
                if (Vector3.Distance(mousePos, lr.GetPosition(lr.positionCount - 2)) > linePointSpacing)
                {
                    //ADD NEW POINT TO LINE
                    currPoints++;
                    lr.positionCount++;
                    lr.SetPosition(lr.positionCount - 1, mousePos);
                    linePoints.Add(mousePos);
                }
            }
            else
            {
                //ADD NEW POINT TO LINE
                currPoints++;
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, mousePos);
                linePoints.Add(mousePos);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            DetectShape();
            lr.positionCount = 0;
            currPoints = 0;
        }
    }

    void DetectShape()
    {
        shapePointGiz.Clear();
        insidePointsList.Clear();
        gizmoPoints.Clear();
        outsidePoints.Clear();

        linePoints = linePoints.Distinct().ToList();

        //CALC CENTER OF LINE POINTS
        Vector3 lineCenter = FindCentroid(linePoints.ToArray());

        //CALC LINE BOUNDS
        (Vector3 lineTopBound, Vector3 lineBottomBound) = FindBounds(linePoints.ToArray());

        //SET SHAPE DATA
        int[] probabilities = new int[shapes.Length];
        for (int i = 0; i < shapes.Length; i++)
        {
            Vector3[] shapePoints = shapes[i];

            //SHAPE CENTRIOD
            Vector3 shapeCenter = FindCentroid(shapePoints);

            //OBJECTS OFFSET
            Vector3 offset = lineCenter - shapeCenter;

            float lineScale = lineTopBound.x - lineBottomBound.x;
            if (lineTopBound.y - lineBottomBound.y > lineScale)
            {
                lineScale = lineTopBound.y - lineBottomBound.y;
            }

            List<Vector3> scaledPoints = new List<Vector3>();
            foreach (Vector3 point in shapePoints)
            {
                Vector3 scaledPoint = (point * (lineScale / 2)) + offset;
                scaledPoints.Add(scaledPoint);
            }

            int insidePoints = 0;
            List<Vector2> expandedPoints = new List<Vector2>();
            for (int n = 0; n < scaledPoints.Count - 1; n++)
            {
                //gizmoPoints.Add(scaledPoints[n]);
                //gizmoPoints.Add(scaledPoints[n + 1]);

                Vector2 direction = scaledPoints[n + 1] - scaledPoints[n];
                Vector2 perpDirection = new Vector2(-direction.y, direction.x).normalized;

                Vector3 TR = scaledPoints[n] + ((Vector3)perpDirection * detectionLeniency);
                Vector3 TL = scaledPoints[n] - ((Vector3)perpDirection * detectionLeniency);

                Vector3 BR = scaledPoints[n + 1] + ((Vector3)perpDirection * detectionLeniency);
                Vector3 BL = scaledPoints[n + 1] - ((Vector3)perpDirection * detectionLeniency);

                shapePointGiz.AddRange(new Vector3[] { TR, TL, BL, BR });

                expandedPoints.AddRange(new Vector2[] { TR, TL, BL, BR }); 
            }

            //LOOP THROUGH EACH POINT
            foreach (Vector3 point in linePoints)
            {
                //SEE IF POINT IS WITHIN RECT
                if (PointInPolygon(expandedPoints.ToArray(), point))
                {
                    if (!insidePointsList.Contains(point))
                    {
                        insidePointsList.Add(point);
                        insidePoints++;
                    }
                }
            } 

            //CALC PROBABILITY OF SHAPE DRAWN
            float probability = (float)insidePoints / (float)currPoints * 100;
            Debug.Log(string.Format("PERCENT: {0} %", probability));

            probabilities[i] = Mathf.RoundToInt(probability);
        }

        int highestProb = 0;
        int probIndex = -1;
        for(int i =0; i < probabilities.Length;i++)
        {
            if(probabilities[i] > highestProb)
            {
                highestProb = probabilities[i];
                probIndex = i;
            }
        }

        //Debug.Log(string.Format("MOST LIKELY: {0} {1}%", probIndex.ToString(),highestProb.ToString()));
        if(highestProb >=40)
        {
            Debug.Log(string.Format("{0} RAN", probIndex.ToString()));
            gizmoPoints.AddRange(shapes[probIndex]);
        }

        linePoints.Clear();
    }

    bool PointInPolygon(Vector2[] Points, Vector2 point)
    {
        // Get the angle between the point and the
        // first and last vertices.
        int max_point = Points.Length - 1;
        float total_angle = GetAngle(Points[max_point], point, Points[0]);

        // Add the angles from the point
        // to each other pair of vertices.
        for (int i = 0; i < max_point; i++)
        {
            total_angle += GetAngle(Points[i], point, Points[i + 1]);
        }
        return (Mathf.Abs(total_angle) > 1);
    }

    float GetAngle(Vector2 a, Vector2 b, Vector2 c)
    {
        // Get the dot product.
        float dot_product = DotProduct(a,b,c);

        // Get the cross product.
        float cross_product = CrossProductLength(a,b,c);

        // Calculate the angle.
        return (float)Mathf.Atan2(cross_product, dot_product);
    }

    public static float CrossProductLength(Vector2 a, Vector2 b, Vector2 c)
    {
        // Get the vectors' coordinates.
        float BAx = a.x - b.x;
        float BAy = a.y - b.y;
        float BCx = c.x - b.x;
        float BCy = c.y - b.y;

        // Calculate the Z coordinate of the cross product.
        return (BAx * BCy - BAy * BCx);
    }

    float DotProduct(Vector2 a, Vector2 b, Vector2 c)
    {
        // Get the vectors' coordinates.
        float BAx = a.x - b.x;
        float BAy = a.y - b.y;
        float BCx = c.x - b.x;
        float BCy = c.y - b.y;

        // Calculate the dot product.
        return (BAx * BCx + BAy * BCy);
    }

    Vector3 FindCentroid(Vector3[] points)
    {
        Vector3 avgPoint = Vector3.zero;
        foreach (Vector3 point in points)
        {
            avgPoint += point;
        }
        return avgPoint / points.Length;
    }

    (Vector3 topBound, Vector3 bottomBound) FindBounds(Vector3[] points)
    {
        Vector3 tempTopBound = Vector3.zero;
        Vector3 tempBottomBound = Vector3.zero;

        foreach (Vector3 point in points)
        {
            if(point.x > tempTopBound.x)
            {
                tempTopBound.x = point.x;
            }
            if(point.y > tempTopBound.y)
            {
                tempTopBound.y = point.y;
            }

            if (point.x < tempBottomBound.x)
            {
                tempBottomBound.x = point.x;
            }
            if (point.y < tempBottomBound.y)
            {
                tempBottomBound.y = point.y;
            }
        }

        return (tempTopBound, tempBottomBound);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Vector3 gizPoint in gizmoPoints)
        {
            Gizmos.DrawWireSphere(gizPoint, 0.1f);
        }

        Gizmos.color = Color.magenta;
        foreach (Vector3 point in outsidePoints)
        {
            Gizmos.DrawWireSphere(point, 0.1f);
        }

        Gizmos.color = Color.blue;
        if (shapePointGiz.Count > 1)
        {
            for (int n = 0; n < shapePointGiz.Count - 1; n+=4)
            {
                int index = n;
                if(n >= shapePointGiz.Count)
                {
                    index = n - shapePointGiz.Count;
                }
                Gizmos.DrawLine(shapePointGiz[n], shapePointGiz[n + 1]);
                Gizmos.DrawLine(shapePointGiz[n + 1], shapePointGiz[n + 2]);
                Gizmos.DrawLine(shapePointGiz[n + 2], shapePointGiz[n+3]);
                Gizmos.DrawLine(shapePointGiz[n + 3], shapePointGiz[n]);
            }
        }
    }
}
