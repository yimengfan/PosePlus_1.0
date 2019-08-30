using System;
using System.Collections.Generic;

using System.Text;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Animator))]
public class Animator_Inspector : Editor
{
    private readonly float fps = 30;
    //
    Dictionary<string, float> anipos = new Dictionary<string, float>();
    public override void OnInspectorGUI()
    {
        if (this.target == null) return;
        base.OnInspectorGUI();
        if (Application.isPlaying) return;

        EditorGUILayout.HelpBox("这里增加一个Inspector,用来检查动画和产生CleanData.Ani", MessageType.Info);

        //GUILayout.Label("1:" + this.serializedObject);
        //GUILayout.Label("2:" + this.target);
        //return;
        var ani = target as Animator;
        FB.PosePlus.AniPlayer con = ani.GetComponent<FB.PosePlus.AniPlayer>();

        if (con == null)
        {
            if (GUILayout.Button("添加CleanData.Ani组件"))
            {
                con = ani.gameObject.AddComponent<FB.PosePlus.AniPlayer>();
                ani.enabled = false;
            }
        }
        else
        {
            GUILayout.Label("已经有CleanData.Ani");

            List<AnimationClip> clips = new List<AnimationClip>();
            //if (GUILayout.Button("Update Clips"))
            //{
            UnityEditor.Animations.AnimatorController cc = ani.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (cc != null)
            {
                FindAllAniInControl(cc, clips);
                GUILayout.Label("拥有动画:" + clips.Count);

                if (GUILayout.Button("重新生成所有 FBAaniClip"))
                {//用最粗暴的方式清理
                    con.Clips = null;
                    foreach (var c in clips)
                    {
                        CloneAni(c, c.frameRate);
                    }

                    return;
                }
                //}
                foreach (var c in clips)
                {
                    if (c == null) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(c.name + "(" + c.length * c.frameRate + ")");
                    if (GUILayout.Button("Create FBAniClip", GUILayout.Width(150)))
                    {
                        CloneAni(c, c.frameRate);
                    }
                    //if (GUILayout.Button("clone fps=16", GUILayout.Width(150)))
                    //{
                    //    CloneAni(c, 16);
                    //}
                    GUILayout.EndHorizontal();
                    if (anipos.ContainsKey(c.name) == false)
                    {
                        anipos[c.name] = 0;
                    }
                    float v = anipos[c.name];
                    v = GUILayout.HorizontalSlider(v, 0, c.length);
                    if (v != anipos[c.name])
                    {
                        anipos[c.name] = v;
                        ani.Play(c.name, 0, v / c.length);
                        ani.updateMode = AnimatorUpdateMode.UnscaledTime;
                        ani.Update(0);
                    }
                }
            }
        }
    }
    void CloneAni(AnimationClip clip, float fps)
    {
        
        var ani = target as Animator;

        //创建CleanData.Ani
        FB.PosePlus.AniClip _clip = ScriptableObject.CreateInstance<FB.PosePlus.AniClip>();
        _clip.boneinfo = new List<string>();//也增加了每个动画中的boneinfo信息.

        //这里重新检查动画曲线，找出动画中涉及的Transform部分，更精确
        List<Transform> cdpath = new List<Transform>();
        AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(clip, true);
        
    
        //Debug.Log("动画骨骼:" + curveDatas.Length);
        foreach (var dd in curveDatas)
        {
            Transform tran = ani.transform.Find(dd.path);

            if (cdpath.Contains(tran) == false)
            {
                if (tran == null)
                {
                    Debug.LogErrorFormat(" 动画:{0} 骨骼不存在:{1}",clip.name,dd.path);
                    continue;
                }
                _clip.boneinfo.Add(dd.path);
                cdpath.Add(tran);
            }
        }
        Debug.LogWarning("curve got path =" + cdpath.Count);


        string path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(clip.GetInstanceID()));
        _clip.name = clip.name;
        _clip.frames = new List<FB.PosePlus.Frame>();
        _clip.loop = clip.isLooping;
        float flen = (clip.length * fps);
        int framecount = (int)flen;
        if (flen - framecount > 0.0001) framecount++;
        //if (framecount < 1) framecount = 1;

        framecount += 1;
        FB.PosePlus.Frame last = null;

        //ani.StartPlayback();
        //逐帧复制
        //ani.Play(_clip.name, 0, 0);
        for (int i = 0; i < framecount; i++)
        {
            ani.Play(_clip.name, 0, (i * 1.0f / fps) / clip.length);
            ani.Update(0);

            last = new FB.PosePlus.Frame(last, i, cdpath);
            _clip.frames.Add(last);
        }
        if (_clip.loop)
        {
            _clip.frames[0].LinkLoop(last);
        }
        Debug.Log("FrameCount." + framecount);

        FB.PosePlus.AniPlayer con = ani.GetComponent<FB.PosePlus.AniPlayer>();

        List<FB.PosePlus.AniClip> clips = null;
        if (con.Clips != null)
        {
            clips = new List<FB.PosePlus.AniClip>(con.Clips);
        }
        else
        {
            clips = new List<FB.PosePlus.AniClip>();
        }
        foreach (var c in clips)
        {
            if (c.name == _clip.name + ".FBAni")
            {
                clips.Remove(c);
                break;
            }
        }

        //ani.StopPlayback();
        string outpath = path + "/" + clip.name + ".FBAni.asset";
        AssetDatabase.CreateAsset(_clip, outpath);
        var src = AssetDatabase.LoadAssetAtPath(outpath, typeof(FB.PosePlus.AniClip)) as FB.PosePlus.AniClip;

        //设置clip

        //FB.CleanData.AniController con = ani.GetComponent<FB.CleanData.AniController>();

        clips.Add(src);
        con.Clips = clips;
    }
    //从一个Animator中获取所有的Animation
    public static void FindAllAniInControl(UnityEditor.Animations.AnimatorController control, List<AnimationClip> list)
    {
        for (int i = 0; i < control.layers.Length; i++)
        {
            var layer = control.layers[i];
            FindAllAniInControl(layer.stateMachine, list);
        }
    }
    static void FindAllAniInControl(UnityEditor.Animations.AnimatorStateMachine machine, List<AnimationClip> list)
    {
        for (int i = 0; i < machine.states.Length; i++)
        {
            var m = machine.states[i].state.motion;
            if (list.Contains(m as AnimationClip) == false)
            {
                list.Add(m as AnimationClip);
            }
        }
        for (int i = 0; i < machine.stateMachines.Length; i++)
        {
            var m = machine.stateMachines[i].stateMachine;
            FindAllAniInControl(m, list);
        }
    }
}

