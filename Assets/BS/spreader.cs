using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spreader : MonoBehaviour
{
    public TextAsset taNoteJson;
    public AudioSource asSong;

    public GameObject prefLeftRed;
    public GameObject prefRightBlue;

    public Transform trLeft;
    public Transform trRight;
    public Transform[] trBlockPosition;

    List<KeyValuePair<float, old.JSONObject>> listTimeNote = new List<KeyValuePair<float, old.JSONObject>>();

    // Start is called before the first frame update
    void Start()
    {
        old.JSONObject obj = new old.JSONObject(taNoteJson.text);
        int notesID = obj.keys.FindIndex(i => i == "_notes");
        old.JSONObject notes = obj.list[notesID];
        foreach(var note in notes.list)
        {
            int timeID = note.keys.FindIndex(i => i == "_time");
            float time = note.list[timeID].f;
            listTimeNote.Add(new KeyValuePair<float, old.JSONObject>(time, note));
        }
        listTimeNote.Sort((x, y) => x.Key.CompareTo(y.Key));

        asSong.Play();
    }

    // Update is called once per frame
    void Update()
    {
        for(; ; )
        {
            if(listTimeNote.Count > 0 && listTimeNote[0].Key - 3f <= asSong.time)
            {
                var info = new noteinfo(listTimeNote[0].Value);
                
                switch(info.type)
                {
                    default: break;
                    case 0:
                        {
                            var go = Instantiate(prefLeftRed);
                            go.transform.position = trLeft.position;
                            go.transform.Translate(0,Random.Range(-100,100)/100f,0);
                            var note = go.GetComponent<note>();
                            note.info = info;
                            note.asSong = asSong;
                        }
                        break;
                    case 1:
                        {
                            var go = Instantiate(prefRightBlue);
                            go.transform.position = trRight.position;
                            go.transform.Translate(0, Random.Range(-100, 100) / 100f, 0);
                            var note = go.GetComponent<note>();
                            note.info = info;
                            note.asSong = asSong;
                        }
                        break;
                }
                listTimeNote.RemoveAt(0);
            }
            else
                break;
        }
    }
}
