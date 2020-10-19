using System.Collections;
using System.Collections.Generic;
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

    private LineRenderer lr;
    private List<Vector3> linePoints = new List<Vector3>();

    private List<Vector3> gizmoPoints = new List<Vector3>();
    private List<Vector3> insidePointsList = new List<Vector3>();

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
                    lr.positionCount++;
                    lr.SetPosition(lr.positionCount - 1, mousePos);
                    linePoints.Add(mousePos);
                }
            }
            else
            {
                //ADD NEW POINT TO LINE
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

        if (Input.GetKeyDown(KeyCode.R))
        {
            //RESET DRAWN LINE
            lr.positionCount = 0;
        }
    }

    void DetectShape()
    {
        insidePointsList.Clear();
        gizmoPoints.Clear();

        //LINE/DRAWING BOUNDS
        Bounds shapeBounds = new Bounds(transform.position, Vector3.zero);
        foreach (Vector3 point in linePoints)
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
        foreach (Vector3 point in straightLine)
        {
            straightLineBounds.Encapsulate(point);
        }

        //OBJECTS OFFSET
        Vector3 offset = shapeBounds.center - straightLineBounds.center;

        List<Vector3> scaledPoints = new List<Vector3>();
        Bounds scaledBounds = new Bounds();
        foreach (Vector3 point in straightLine)
        {
            Vector3 scaledPoint = (point * shapeBounds.extents.y) + (offset * 2);
            scaledPoints.Add(scaledPoint);
            scaledBounds.Encapsulate(scaledPoint);
        }

        int insidePoints = 0;
        for (int n = 0; n < scaledPoints.Count - 1; n++)
        {
            gizmoPoints.Add(scaledPoints[n]);
            gizmoPoints.Add(scaledPoints[n + 1]);

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

            //gizmoPoints.AddRange(new Vector3[] { TL, TR, BL, BR });

            //LOOP THROUGH EACH POINT
            foreach (Vector3 point in linePoints)
            {
                //SEE IF POINT IS WITHIN RECT
                if (ContainsPoint(new Vector2[] { TL, TR, BL, BR }, point))
                {
                    insidePointsList.Add(point);
                    insidePoints++;
                }
            }
        }

        //CALC PROBABILITY OF SHAPE DRAWN
        float probability = Mathf.Round((float)insidePoints / (float)linePoints.Count * 100);
        Debug.Log(string.Format("PERCENT: {0} %", probability));

        linePoints.Clear();
    }

    bool ContainsPoint(Vector2[] polyPoints, Vector2 p)
    {
        int j = polyPoints.Length - 1;
        bool inside = false;
        for (int i = 0; i < polyPoints.Length; j = i++)
        {
            Vector2 pi = polyPoints[i];
            Vector2 pj = polyPoints[j];
            if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }
        return inside;
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
    }
}
