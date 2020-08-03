using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class saber : MonoBehaviour
{
    public Camera cam;

    public Transform trSaberStart;
    public Transform trSaberCenter;
    public Transform trSaberEnd;
    Queue<Vector3> vSaberCenterLog = new Queue<Vector3>();

    Vector3 cross;

    private void Start()
    {
    }

    private void Update()
    {
        vSaberCenterLog.Enqueue(trSaberCenter.position);
        foreach(var v in vSaberCenterLog)
        {
            Debug.DrawLine(v, trSaberStart.position, Color.yellow);//, 0.1f);
            Debug.DrawLine(v, trSaberEnd.position, Color.yellow);//, 0.1f);
        }
        while (vSaberCenterLog.Count > 15)
            vSaberCenterLog.Dequeue();


        //Vector2 MousePosition;
        //MousePosition = (Input.mousePosition + new Vector3(-100, -100)) / 400;
        ////MousePosition = cam.ScreenToWorldPoint(MousePosition);
        //
        //Debug.DrawLine(transform.position, MousePosition, Color.red, 1);
        ////cross = (Vector3)MousePosition - transform.position;
        //cross = Vector3.Cross(transform.position, MousePosition);
        //transform.position = MousePosition;


    }


    private void OnTriggerEnter(Collider other)
    {
        print("트리거엔터");
        {
            var pos = other.transform.position;
            var p = new Plane(
            a: other.transform.InverseTransformPoint(trSaberStart.position),
            b: vSaberCenterLog.Dequeue(),
            c: other.transform.InverseTransformPoint(trSaberEnd.position)
            );
            GameObject[] gameObjects = MeshCut.Cut(other.gameObject, p, other.gameObject.GetComponent<MeshRenderer>().material);
            print("잘림갯수 : " + gameObjects.Length);
            foreach (var go in gameObjects)
            {
                Destroy(go.GetComponent<note>());
                go.AddComponent<MeshCollider>().convex = true;
                var rigi = go.GetComponent<Rigidbody>();
                if (rigi == null)
                    rigi = go.AddComponent<Rigidbody>();
                rigi.useGravity = true;
                rigi.AddExplosionForce(50, pos, 0, 0, ForceMode.Impulse);
                //Debug.DrawLine(pos, pos + Vrand, Color.red, 5);
            }
        }
    }
}
