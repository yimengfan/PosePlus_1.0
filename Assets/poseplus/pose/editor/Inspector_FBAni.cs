using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Code.Game.poseplus.pose.editor;
using FB.PosePlus;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(FB.PosePlus.AniPlayer))]
public class FBAni_Inspector : Editor
{
    //
    DateTime last = DateTime.Now;
    string newaniname = "";
    UnityEngine.Transform rootBone = null;

    void FillAniBoneInfo(Transform root, FB.PosePlus.AniPlayer controller, FB.PosePlus.AniClip ani)
    {
        string path = getPath(root, controller.transform);
        if (ani.boneinfo == null)
            ani.boneinfo = new List<string>();
        if (path.Equals(controller.name) == false)
            ani.boneinfo.Add(path);
        foreach (Transform t in root)
        {
            FillAniBoneInfo(t, controller, ani);
        }
    }

    string getPath(Transform _cur, Transform _base)
    {
        //if (_base == _cur)
        //    throw new Exception("root bone is _cur tran.");
        string name = _cur.name; //hip
        while (_cur.parent != null)
        {
            if (_cur.parent == _base)
                break;
            _cur = _cur.parent;
            name = _cur.name + "/" + name;
        }

        return name;
    }

    bool bShowTree = false;
    bool baseinfo = false;
    private FB.PosePlus.AniPlayer con;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (con == null)
        {
            EditorApplication.update += this._OnUpdate;
        }

        con = target as FB.PosePlus.AniPlayer;

        if (con == null) return;
        if (con.Clips == null) con.Clips = new List<FB.PosePlus.AniClip>();


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("向左"))
        {
            con.SetDir(-1);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("向右"))

        {
            con.SetDir(1);
        }

        GUILayout.EndHorizontal();
        {
            foreach (var c in con.Clips)
            {
                if (c == null) continue;
                GUILayout.BeginHorizontal();
                if (c.frames == null) continue;
                GUILayout.Label(c.name + "(" + (c.loop ? "loop" : "") + c.frames.Count + ")");
                if (GUILayout.Button("play", GUILayout.Width(150)))
                {
                    con.Play(c.name);
                    //con.Play();
                    bPlay = true;
                    //return;
                    //CloneAni(c);
                }

                if (GUILayout.Button("cross 0.2", GUILayout.Width(150)))
                {
                    con.Play(c.name);

                    bPlay = true;
                }


                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(40);
                GUILayout.BeginVertical();
                foreach (var sub in c.subclips)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(sub.name + (sub.loop ? "[Loop]" : "") + "(" + (sub.endframe - sub.startframe + 1) +
                                    ")");
                    if (GUILayout.Button("play", GUILayout.Width(100)))
                    {
                        con.Play(c.name, sub.name);
                        bPlay = true;
                    }

                    if (GUILayout.Button("cross 0.2", GUILayout.Width(100)))
                    {
                        con.Play(c.name, sub.name);

                        bPlay = true;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }
        {
            //动画控制
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("stopAni", GUILayout.Width(150)))
            {
                bPlay = false;
                //con.Stop();
            }

            if (GUILayout.Button("AddTime", GUILayout.Width(150)))
            {
                con._OnUpdate(0.04f);
            }

            GUILayout.EndHorizontal();
        }
        {
            if (GUILayout.Button("显示/隐藏Box"))
            {
                var ani = target as FB.PosePlus.AniPlayer;
                ani.isShowBoxLine = !ani.isShowBoxLine;

                var trans = con.transform.Find("_boxes");
                if (trans == null) Debug.LogError("没有找到_boxes");
                foreach (Transform t in trans)
                {
                    var o = t.gameObject;

                    if (ani.isShowBoxLine)
                    {
                        if (!o.GetComponent<Collider_Vis>())
                            o.AddComponent<Collider_Vis>();
                        if (!o.GetComponent<LineRenderer>())
                            o.AddComponent<LineRenderer>();
                        if (!o.GetComponent<MeshRenderer>())
                            o.AddComponent<MeshRenderer>();
                        ani.SetBoxColor(o);

                        //o.GetComponent<Collider_Vis>().updateColl();
                    }
                    else
                    {
                        if (o.GetComponent<Collider_Vis>())
                            DestroyImmediate(o.GetComponent<Collider_Vis>());
                        if (o.GetComponent<LineRenderer>())
                            DestroyImmediate(o.GetComponent<LineRenderer>());
                        if (o.GetComponent<MeshRenderer>())
                            DestroyImmediate(o.GetComponent<MeshRenderer>());
                    }
                }

                var _trans = con.transform.Find("_dotes");
                if (trans == null) Debug.LogError("没有找到_dotes");
                foreach (Transform t in _trans)
                {
                    var o = t.gameObject;

                    if (ani.isShowBoxLine)
                    {
/*                        if (!o.GetComponent<LineRenderer>())*/
                        o.GetComponent<LineRenderer>().enabled = true;
                    }
                    else
                    {
/*                        if (o.GetComponent<LineRenderer>())*/
                        o.GetComponent<LineRenderer>().enabled = false;
                    }
                }
            }

            if (GUILayout.Button("匹配所有动画的骨骼"))
            {
                var win = EditorWindow.GetWindow<EditorWindow_FBAniCheck>("动作检测");
                win.Show(target as AniPlayer);
            }

            GUI.color = Color.red;
            if (GUILayout.Button("寻找丢失动画"))
            {
                FindLoseAni();
            }

            GUI.color = GUI.contentColor;
        }
    }

    bool bPlay = false;

    int calcbonehash(FB.PosePlus.AniClip clip)
    {
        string b = "";
        foreach (var bi in clip.boneinfo)
        {
            b += bi + "|";
        }

        return b.GetHashCode();
    }

    void MatchBone()
    {
        var con = target as FB.PosePlus.AniPlayer;

        int hashfirst = calcbonehash(con.Clips[0]);
        foreach (var ani in con.Clips)
        {
            int hash = calcbonehash(ani);
            if (hash != hashfirst)
            {
                Debug.LogWarning("动画：" + ani.name + "骨骼与" + con.Clips[0].name + "不匹配");
                ani.MatchBone(con.Clips[0]);
                EditorUtility.SetDirty(ani);
            }
        }
    }

    void _OnUpdate()
    {
        if (bPlay)
        {
            DateTime _now = DateTime.Now;
            float delta = (float) (_now - last).TotalSeconds;
            last = _now;
            con._OnUpdate(delta);
            // Repaint();
        }
    }


    /// <summary>
    /// 寻找1个角色丢失动画
    /// </summary>
    void FindLoseAni()
    {
        var ani = this.target as AniPlayer;
        var parent = EditorUtility.GetPrefabParent(ani.gameObject);
        var prefabPath = AssetDatabase.GetAssetPath(parent);
//        Debug.Log(Selection.activeGameObject.name);
        Debug.Log(prefabPath);
        var prefabFs = Directory.GetFiles(Path.GetDirectoryName(prefabPath), "*.prefab", SearchOption.AllDirectories);

        GameObject temp = new GameObject("temp");
        int i = 0;
        foreach (var f in prefabFs)
        {
            i++;
            var o = AssetDatabase.LoadAssetAtPath<GameObject>(f);
            var prefab = PrefabUtility.InstantiatePrefab(o) as GameObject;
            //
            prefab.transform.SetParent(temp.transform);
            
            //开始处理transform下所有东西
            var skinmesh = prefab.transform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinmesh == null)
            {
                Debug.LogError("找不到可用的skinmesh目录");
                return;
            }
            //找到mesh所在目录，寻找所有的.fbani
            var path = AssetDatabase.GetAssetPath(skinmesh.sharedMesh);
            var direct = Path.GetDirectoryName(path); //.Replace("Assets",Application.dataPath);
            var assetFs = Directory.GetFiles(direct, "*.asset", SearchOption.AllDirectories);
//            Debug.Log("文件数:" + assetFs.Length);
            var _ani = prefab.GetComponent<AniPlayer>();
            if (_ani == null)
            {
                continue;
            }
            if (assetFs.Length > 0)
            {
                _ani.Clips = new List<AniClip>();
            }
            //找到所有Asset加载 赋值
            foreach (var asset in assetFs)
            {
                var aniclip = AssetDatabase.LoadAssetAtPath<AniClip>(asset);
                if (aniclip != null)
                {
                    _ani.Clips.Add(aniclip);
                }
            }
            PrefabUtility.ApplyPrefabInstance(prefab, InteractionMode.AutomatedAction);

            EditorUtility.DisplayProgressBar("寻找中:", "寻找:" + i + "/" + prefabFs.Length, i / prefabFs.Length);
        }
        EditorUtility.ClearProgressBar();
        GameObject.DestroyImmediate(temp);
    }
}