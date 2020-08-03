using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct noteinfo
{
    public float time;
    public int type;

    public noteinfo(old.JSONObject jsonNote)
    {
        /*
         "_time":4.300000190734863,
         "_lineIndex":0,
         "_lineLayer":1,
         "_type":0,
         "_cutDirection":4
         */
        time = jsonNote.GetJSONObject("_time").f;
        type = (int)jsonNote.GetJSONObject("_type").i;
    }
}
public class note : MonoBehaviour
{
    public AudioSource asSong;
    public noteinfo info;

    // Update is called once per frame
    void Update()
    {
        float newZ = Mathf.LerpUnclamped(0, 50, (info.time - asSong.time)*0.1f);

        var newpos = transform.position;
        newpos.z = newZ;
        transform.position = newpos;

        if(false)//newZ < 0)
        {
            var pos = transform.position;

            var Vrand = new Vector3(Random.Range(10, -10), Random.Range(10, -10), Random.Range(10, -10));
            GameObject[] gameObjects = MeshCut.Cut(gameObject, pos, Vrand, GetComponent<MeshRenderer>().material);
            print("잘림갯수 : " + gameObjects.Length);
            foreach(var go in gameObjects)
            {
                var rigi = go.AddComponent<Rigidbody>();
                Destroy(go.GetComponent<note>());
                go.AddComponent<MeshCollider>().convex = true;
                rigi.AddExplosionForce(100, pos, 0, 0, ForceMode.Impulse);
                Debug.DrawLine(pos, pos + Vrand, Color.red,5);
            }
        }
    }
}
