using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class misc
{
    public static old.JSONObject GetJSONObject(this old.JSONObject json, string key)
    {
        int id = json.keys.IndexOf(key);
        if (id < 0) return null;
        return json.list[id];
    }
}
