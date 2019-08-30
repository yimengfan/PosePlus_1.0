using System;
using System.Collections.Generic;
using FB.PosePlus;
using UnityEditor;
using UnityEngine;

namespace Code.Game.poseplus.pose.editor
{
    public class EditorWindow_FBAniCheck : EditorWindow
    {
        private AniPlayer ani;

        public void Show(AniPlayer ani)
        {
            this.ani = ani;
            base.Show();
        }


        private List<string> popList = null;
        private int lastSelect = 0;
        private List<string> allBones = null;

        Vector2 pos = Vector2.zero;

        
        private void OnGUI()
        {

            if (popList == null)
            {
                popList = new List<string>();

                foreach (var c in ani.Clips)
                {
                    popList.Add(c.name);
                }

                lastSelect = popList.FindIndex((s) => s.Contains("idle."));
                if (lastSelect < 0)
                {
                    lastSelect = 0;
                }
                
                SeachAllBones(popList[lastSelect]);
                
            }
            else if (calcMap.Count == 0)
            {
                SeachAllBones(popList[lastSelect]);
            }

            var last = lastSelect;
            GUILayout.Label("请选择基准骨骼动作（一般为idle）");
            lastSelect = EditorGUILayout.Popup(lastSelect, popList.ToArray(),GUILayout.Width(150));
            if (lastSelect != last)
            {
                SeachAllBones(popList[lastSelect]);
            }
            
            
            pos = GUILayout.BeginScrollView(pos);
            {
                foreach (var item in calcMap)
                {
                    
                    var ret = item.Value;
                    GUI.color = Color.green;
                    GUILayout.Label("clip:"  + item.Key);
                    GUI.color = GUI.contentColor;
                    GUILayout.Label(ret.info);

                    GUILayout.Label("检测结果:");
                    if (ret.errorBones.Count == 0)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("通过检测");
                        GUI.color = GUI.contentColor;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("错误骨骼开始于(默认显示前10组):");
                        GUILayout.Label("异常数:" + ret.errorBones.Count);
                        GUI.color = GUI.contentColor;
                        var count = Mathf.Min(20, ret.errorBones.Count);

                        if (GUILayout.Button(">>",GUILayout.Width(30)))
                        {
                            var wrong = ani.Clips.Find((c) => c.name == item.Key).boneinfo;
                            EditorWindow_FBAniCheck_SubCompare.Show(allBones,wrong );
                        }
                        for (int i = 0; i < count; i+=2)
                        {
                            GUILayout.Label(string.Format("第{0}对:",i/2 +1));
                            GUILayout.Label("正确骨骼:"+ret.errorBones[i]);
                            GUILayout.Label("动作骨骼:"+ret.errorBones[i +1]);
                            GUILayout.Space(2);
                        }
                    }
                    GUILayout.Label("匹配骨骼情况:");
                    
                    if (ret.notExsitBones.Count == 0)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("通过检测");
                        GUI.color = GUI.contentColor;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("模型中无下列骨骼:");
                        GUILayout.Label("异常数:" + ret.notExsitBones.Count);
                        GUI.color = GUI.contentColor;
                        
                        var count = Mathf.Min(20, ret.notExsitBones.Count);
                        for (int i = 0; i < count; i++)
                        {
                            GUILayout.Label("骨骼:"+ret.notExsitBones[i]);
                            GUILayout.Space(2);
                        }
                    }

                    DrawLineH(Color.white);
                }
            }
            GUILayout.EndScrollView();
        }


        public void SeachAllBones(string name)
        {
            foreach (var c in ani.Clips)
            {
                if (c.name == name)
                {
                    allBones = c.boneinfo;
                }
            }

            foreach (var c in ani.Clips)
            {
                calcMap[c.name] = CheckBones(c);
            }


        }

        
        public class  retData
        {
            public string info = "";
            public List<string> errorBones = new List<string>();
            public List<string> notExsitBones = new List<string>();
        }
        
        Dictionary<string,retData> calcMap = new Dictionary<string, retData>();
        
        public retData CheckBones(AniClip clip)
        {
            retData ret = new retData();
            ret.info = string.Format("人物骨骼数:{0},动作骨骼数:{1}", allBones.Count, clip.boneinfo.Count);
            

            var count = Mathf.Min(clip.boneinfo.Count, allBones.Count);
            //
            for (int i = 0; i < count; i++)
            {
                if (allBones[i] != clip.boneinfo[i])
                {
                    ret.errorBones.Add(allBones[i]);
                    ret.errorBones.Add(clip.boneinfo[i]);
                }

                //丢失节点
                if (ani.transform.Find(clip.boneinfo[i]) ==null)
                {
                    ret.notExsitBones.Add(clip.boneinfo[i]);
                }
                
            }

            return ret;
        }
        
        public static void DrawLineH(Color color, float height = 4f)
        {

            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(height);
        }

        public static void DrawLineV(Color color, float width = 4f)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(width);
        }
    }
}