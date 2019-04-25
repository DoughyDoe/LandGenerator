using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(MapGen))]
public class MapGenEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        MapGen mapGen = (MapGen)target;//cast target to a MapGen
        if(DrawDefaultInspector())
        {
            if(mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        if(GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
