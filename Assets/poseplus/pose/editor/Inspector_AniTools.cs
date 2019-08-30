using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(FB.PosePlus.Dev_FBaniTools))]
public class Inspector_AniTools : Editor
{
    FB.PosePlus.Dev_FBaniTools aniTools;
    public override void OnInspectorGUI()
    {
        aniTools = target as FB.PosePlus.Dev_FBaniTools;
        base.OnInspectorGUI();

        if(GUILayout.Button("数据编辑"))
        {
            if(aniTools.clip_a == null || aniTools.clip_b == null)
            {
                EditorUtility.DisplayDialog("错误", "加上数据好吧!", "我错了大人！");
                return;
            }
            EditorWindow_AniTools.Show(aniTools.clip_a, aniTools.clip_b);
        }
    }



}