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
    public Transform trailFX;

    [Range(0.01f, 1)]
    public float linePointSpacing = 0.25f;
    public float detectionLeniency = 0.3f;

    private LineRenderer lr;
    private List<Vector3> linePoints = new List<Vector3>();

    private List<Vector3> gizmoPoints = new List<Vector3>();
    private List<Vector3> insidePointsList = new List<Vector3>();
    private List<Vector3> shapePointGiz = new List<Vector3>();

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

            trailFX.position = mousePos;

            if (!trailFX.gameObject.activeInHierarchy)
            {
                trailFX.gameObject.SetActive(true);
            }

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
            lr.positionCount = 0;
            trailFX.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            //RESET DRAWN LINE
            lr.positionCount = 0;
        }
    }

    void DetectShape()
    {
        shapePointGiz.Clear();
        insidePointsList.Clear();
        gizmoPoints.Clear();

        //CALC CENTER OF LINE POINTS
        Vector3 lineCenter = FindCentroid(linePoints.ToArray());

        //CALC LINE BOUNDS
        (Vector3 lineTopBound, Vector3 lineBottomBound) = FindBounds(linePoints.ToArray());

        //SET SHAPE DATA
        Vector3[] shapePoints = Shapes.N;

        //SHAPE CENTRIOD
        Vector3 shapeCenter = FindCentroid(shapePoints);

        //OBJECTS OFFSET
        Vector3 offset = lineCenter - shapeCenter;

        float lineScale = lineTopBound.x - lineBottomBound.x;
        if(lineTopBound.y - lineBottomBound.y > lineScale)
        {
            lineScale = lineTopBound.y - lineBottomBound.y;
        }

        List<Vector3> scaledPoints = new List<Vector3>();
        foreach (Vector3 point in shapePoints)
        {
            Vector3 scaledPoint = (point*(lineScale/2)) + offset;
            scaledPoints.Add(scaledPoint);
        }

        int insidePoints = 0;
        for (int n = 0; n < scaledPoints.Count - 1; n++)
        {
            //gizmoPoints.Add(scaledPoints[n]);
            //gizmoPoints.Add(scaledPoints[n + 1]);

            Vector2 direction = scaledPoints[n+1] - scaledPoints[n];
            Vector2 perpDirection = new Vector2(-direction.y, direction.x).normalized;

            Vector3 TR = scaledPoints[n] + ((Vector3)perpDirection * detectionLeniency);
            Vector3 TL = scaledPoints[n] - ((Vector3)perpDirection * detectionLeniency);  

            Vector3 BR = scaledPoints[n + 1] + ((Vector3)perpDirection * detectionLeniency);
            Vector3 BL = scaledPoints[n + 1] - ((Vector3)perpDirection * detectionLeniency);

            shapePointGiz.AddRange(new Vector3[] { TL, TR, BR, BL });
            gizmoPoints.AddRange(new Vector3[] { TL, TR, BR,BL });

            //LOOP THROUGH EACH POINT
            foreach (Vector3 point in linePoints)
            {
                //SEE IF POINT IS WITHIN RECT
                if (ContainsPoint(new Vector2[] { TL, TR, BR, BL }, point))
                {
                    if (!insidePointsList.Contains(point))
                    {
                        insidePointsList.Add(point);
                        insidePoints++;
                    }
                }
            }
        }

        //CALC PROBABILITY OF SHAPE DRAWN
        float probability = (float)insidePoints / (float)linePoints.Count * 100;
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

    Vector3 FindCentroid(Vector3[] points)
    {
        Vector3 avgPoint = Vector3.zero;
        foreach (Vector3 point in points)
        {
            avgPoint += point;
        }
        return avgPoint / linePoints.Count;
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

        Gizmos.color = Color.green;
        foreach (Vector3 point in insidePointsList)
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
