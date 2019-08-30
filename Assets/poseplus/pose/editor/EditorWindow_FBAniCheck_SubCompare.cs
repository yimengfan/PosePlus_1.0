using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Code.Game.poseplus.pose.editor
{
    public class EditorWindow_FBAniCheck_SubCompare : EditorWindow
    {
        private static List<string> right;
        static List<string> wrong;
        private static EditorWindow_FBAniCheck_SubCompare _win;
        static public void Show(List<string> right,List<string> wrong)
        {
            EditorWindow_FBAniCheck_SubCompare.right = right;
            EditorWindow_FBAniCheck_SubCompare.wrong = wrong;

            if (_win == null)
            {
                _win = new EditorWindow_FBAniCheck_SubCompare();
                _win.Show();
            }
        }

   
        Vector2 pos = Vector2.zero;
        private void OnGUI()
        {
            if(right==null||  wrong==null) return;
            

            var max = Mathf.Max(right.Count, wrong.Count);

            pos = GUILayout.BeginScrollView(pos);
            for (int i = 0; i < max; i++)
            {
                string r = "--";
                string w = "--";
                if (i<right.Count)
                {
                    r = right[i];
                }

                if (i<wrong.Count)
                {
                    w = wrong[i];
                }
               
                {
                    if (r == w)
                    {
                        GUI.color = Color.green;
                    }
                    
                    GUILayout.Label(r);
                    if (r != w)
                    {
                        GUI.color = Color.red;
                    }
                    GUILayout.Label(w);
                 
                }
                GUI.color = GUI.contentColor;
                //
                GUILayout.Label("-------------------------------------------------");


            }
            GUILayout.EndScrollView();

        }
    }
}