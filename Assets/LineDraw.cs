using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.iOS.Extensions.Common;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class LineDraw : MonoBehaviour
{
    public Camera cam;
    public float detectionLeniency = 0.3f;

    private LineRenderer lr;
    private List<Vector3> linePoints = new List<Vector3>();

    private List<Vector3> gizmoPoints = new List<Vector3>();
    private List<Vector3> insidePointsList = new List<Vector3>();

    private Bounds shapeBound = new Bounds();

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            if (lr.positionCount > 2)
            {
                if (Vector3.Distance(mousePos, lr.GetPosition(lr.positionCount - 2)) > 0.25f)
                {
                    lr.positionCount++; 
                    lr.SetPosition(lr.positionCount - 1, mousePos);
                    linePoints.Add(mousePos);
                }
            }
            else
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, mousePos);
                linePoints.Add(mousePos);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            DetectShape();
            //lr.positionCount = 0;
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            lr.positionCount = 0;
        }
    }

    void DetectShape()
    {
        insidePointsList.Clear();
        gizmoPoints.Clear();

        //LINE/DRAWING BOUNDS
        Bounds shapeBounds = new Bounds(transform.position, Vector3.zero);
        foreach(Vector3 point in linePoints)
        {
            shapeBounds.Encapsulate(point);
        }

        //NORMALISED SHAPE POINTS
        Vector3[] straightLine = new Vector3[] { 
            new Vector3(0, 1, 0), 
            new Vector3(0, -1, 0) 
        };

        //SHAPE BOUNDS
        Bounds straightLineBounds = new Bounds();
        foreach(Vector3 point in straightLine)
        {
            straightLineBounds.Encapsulate(point);
        }

        //OBJECTS OFFSET
        Vector3 offset = shapeBounds.center - straightLineBounds.center;
        float xOff = straightLineBounds.center.x - shapeBounds.center.x;
        float yOff = straightLineBounds.center.y - shapeBounds.center.y;

        Vector3 newOff = new Vector3(xOff, yOff);

        List<Vector3> scaledPoints = new List<Vector3>();
        Bounds scaledBounds = new Bounds();
        foreach (Vector3 point in straightLine)
        {
            Vector3 scaledPoint = (point * shapeBounds.extents.y) - newOff;
            scaledPoints.Add(scaledPoint);
            scaledBounds.Encapsulate(scaledPoint);
        }

        int insidePoints = 0;
        shapeBound = new Bounds(shapeBounds.center, Vector3.zero);
        for (int n = 0; n < scaledPoints.Count - 1; n++)
        {
            gizmoPoints.Add(scaledPoints[n]);
            gizmoPoints.Add(scaledPoints[n+1]);

            //GET DISTANCE BETWEEN POINTS
            float pointDist = Vector3.Distance(scaledPoints[n], scaledPoints[n + 1]);

            //CALCULATE ANGLE FROM ONE POINT
            float angle = Mathf.Acos(scaledPoints[n + 1].x / pointDist);
            //90 DEGREES OUT FROM POINT (EXTENTS)
            float upAngle = angle + 90;
            float downAngle = angle - 90;

            //CREATE RECTANGLE TOP POINTS
            Vector3 TL = scaledPoints[n] + new Vector3(Mathf.Cos(upAngle) * detectionLeniency, Mathf.Sin(upAngle) * detectionLeniency);
            Vector3 TR = scaledPoints[n] + new Vector3(Mathf.Cos(downAngle) * detectionLeniency, Mathf.Sin(downAngle) * detectionLeniency);

            //CALCULATE ANGLE FROM OTHER POINT
            float angle2 = Mathf.Acos(scaledPoints[n].x / pointDist);
            //90 DEGREES OUT FROM POINT (EXTENTS)
            float upAngle2 = angle2 + 90;
            float downAngle2 = angle2 - 90;

            //CREATE RECTANGLE BOTTOM POINTS
            Vector3 BL = scaledPoints[n + 1] + new Vector3(Mathf.Cos(upAngle2) * detectionLeniency, Mathf.Sin(upAngle2) * detectionLeniency);
            Vector3 BR = scaledPoints[n + 1] + new Vector3(Mathf.Cos(downAngle2) * detectionLeniency, Mathf.Sin(downAngle2) * detectionLeniency);

            shapeBound.Encapsulate(scaledPoints[n]);
            shapeBound.Encapsulate(scaledPoints[n + 1]);
            shapeBound.Encapsulate(TR);
            shapeBound.Encapsulate(TL);
            shapeBound.Encapsulate(BR);
            shapeBound.Encapsulate(BL);
            //gizmoPoints.AddRange(new Vector3[] { TL, TR, BL, BR });

            //LOOP THROUGH EACH POINT
            foreach (Vector3 point in linePoints)
            {
                //SEE IF POINT IS WITHIN RECT
                //   if (pointInRect(point, new Vector3[] { TL, TR, BL, BR }))
                //  {
                //     insidePointsList.Add(point);
                //    insidePoints++;
                // }

                if (shapeBound.Contains(point))
                {
                    insidePointsList.Add(point);
                    insidePoints++;
                }
            }
        }

        float probability =Mathf.Round((float)insidePoints / (float)linePoints.Count *100);

        Debug.Log(string.Format("PERCENT: {0} %", probability));
        Debug.Log(insidePoints.ToString() + "/" + linePoints.Count.ToString());

        linePoints.Clear();
    }

    bool pointInRect(Vector3 point, Vector3[] rectVerts)
    {
        foreach (Vector3 vert in rectVerts)
        {
            for (int n = 0; n < rectVerts.Length - 1; n++)
            {
                if (doIntersect(point, vert, rectVerts[n], rectVerts[n + 1]))
                {
                    //OUTSIDE
                    return false;
                }
            }
        }

        //INSIDE (no lines crossed)
        return true;
    }

    bool doIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        // Find the four orientations needed for general and 
        // special cases 
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases 
    }

    int orientation(Vector3 p, Vector3 q, Vector3 r)
    {
        int val = Mathf.RoundToInt((q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y));

        if (val == 0) return 0; // colinear 

        return (val > 0) ? 1 : 2; // clock or counterclock wise 
    }

    bool onSegment(Vector3 p, Vector3 q, Vector3 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
            return true;

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach(Vector3 gizPoint in gizmoPoints)
        {
            Gizmos.DrawWireSphere(gizPoint, 0.1f);
        }

        Gizmos.color = Color.green;
        foreach(Vector3 point in insidePointsList)
        {
            Gizmos.DrawWireSphere(point, 0.1f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(shapeBound.center, shapeBound.size);
    }
}
