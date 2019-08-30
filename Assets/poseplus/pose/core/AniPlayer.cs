using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FB.PosePlus
{
    public enum AnimationState
    {
        OnUpdate,
        OnPlayEnd,
    }

    //新的动画控制器，相比封闭的animator，建立一个更开放自由的模式
    public class AniPlayer : MonoBehaviour
    {
        /// <summary>
        /// 这里是加载接口,
        /// </summary>
        static public IAniplayerResourceLoader ResourceLoader = null;

        private void Awake()
        {
            isPlayRunTime = true;
        }

        /// <summary>
        /// 所有的动画片段
        /// </summary>
        public List<AniClip> Clips;

        /// <summary>
        /// 当前播放动画
        /// </summary>
        public AniClip CurClip
        {
            get { return lastClip; }
        }

        /// <summary>
        /// 当前帧id
        /// </summary>
        public int CurAniFrame
        {
            get { return lastframe; }
        }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPauseAnimation
        {
            get { return isPauseAnimation; }
        }


        bool isPlayRunTime = false; //是否播放

        private AniClip lastClip; //当前剪辑
        int lastframe = -1; //当前帧

        int frameCounter = -1; // 当前总帧数
        bool bLooped = false;
        int startframe;
        int endframe;
        float _crossTimer = -1;
        float _crossTimerTotal = 0;
        Frame crossFrame = null; //用来混合用的帧


        #region 动画处理

        /// <summary>
        /// 获取一个动画片段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AniClip GetClip(string name)
        {
            var ns = name.Split('.');
            name = ns[0];
            //
            if (Clips == null || Clips.Count == 0)
            {
                return null;
            }

            var clip = Clips.Find((c) => c.name.Contains(name + "."));

            return clip;
        }


        /// <summary>
        /// 默认动画名,
        /// </summary>
        private string defaultAnimationName = null;

        /// <summary>
        /// 设置一个 默认播放的动作，
        /// 当前角色,停止1帧以上,就会播放这个动作.
        /// </summary>
        /// <param name="name"></param>
        public void SetDefaultAnimation(string name)
        {
            defaultAnimationName = name;
        }


        private bool isPauseAnimation = false;

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void StopAnimation()
        {
            isPauseAnimation = true;
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        public void StartAnimation()
        {
            isPauseAnimation = false;
        }


        /// <summary>
        /// 对外的播放接口
        /// </summary>
        /// <param name="clipName">动画名</param>
        /// <param name="subclipName">子动画名</param>
        /// <param name="cross">动画融合时间,0,代表不融合</param>
        public void Play(string clipName, string subclipName = null, float cross = 0.2f)
        {
            if (string.IsNullOrEmpty(clipName) == false)
            {
                //
                var _clip = GetClip(clipName);
                if (_clip == null)
                {
                    Debug.LogError("No clip:" + clipName);
                    return;
                }

                SubClip _subclip = null;
                if (string.IsNullOrEmpty(subclipName) == false)
                {
                    _subclip = _clip.GetSubClip(subclipName);
                }


                //这里不直接调用了，改先缓存起来,等update中调用 并设置状态
                nextPlayAniClip = _clip;
                nextPlaySubAniClip = _subclip;
                nextPlayCrosstimer = cross;
            }
        }

        private AniClip nextPlayAniClip = null;
        private SubClip nextPlaySubAniClip = null;
        private float nextPlayCrosstimer = -1;

        /// <summary>
        /// 播放一个动画片段
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="subClip"></param>
        /// <param name="crosstimer"></param>
        private void Play(AniClip clip, SubClip subClip = null, float crosstimer = 0)
        {
            //清除特效
            {
                for (int i = 0; i < effectLifeList.Count; i++)
                {
                    ResourceLoader.CloseEffectLooped(effectLifeList[i].effid);
                }

                effectLifeList.Clear();
                ResourceLoader.CleanAllEffect();
            }

            //开始播放
            if (subClip != null) //优先播放子动画
            {
                bLooped = subClip.loop;
                startframe = (int) subClip.startframe;
                endframe = (int) subClip.endframe;
            }
            else if (clip != null) //子动画不存在.则播放父动画
            {
                bLooped = clip.loop;
                startframe = 0;
                endframe = (clip.aniFrameCount - 1);
            }


            //切换动作 不需要过渡
            if (crosstimer <= 0)
            {
                this._crossTimer = -1;
                crossFrame = null;

                lastClip = clip;
                lastframe = startframe;

                SetPose(clip, startframe, true);
                //修复设置后立马推帧问题
                timer = 0f;
            }
            //切换动作需要过渡
            else
            {
                //当前动作没做完
                if (lastClip != null && lastframe >= 0 && lastframe < lastClip.frames.Count)
                {
                    RecCrossFrame();
                    lastClip = clip;
                    lastframe = startframe;
                    //修复设置后立马推帧问题
                    timer = 0f;

                    this._crossTimerTotal = this._crossTimer = crosstimer;
                }
                //当前动作做完了
                else
                {
                    lastClip = clip;
                    lastframe = startframe;

                    SetPose(clip, startframe, true);
                    //修复设置后立马推帧问题
                    timer = 0f;
                }
            }
        }

        //重新计算混合帧
        private void RecCrossFrame()
        {
            if (this._crossTimer >= 0 && crossFrame != null)
            {
                Frame f = new Frame();

                float l = 1.0f - _crossTimer / _crossTimerTotal;
                crossFrame = Frame.Lerp(crossFrame, lastClip.frames[lastframe], l);
            }
            else
            {
                crossFrame = lastClip.frames[lastframe];
            }
        }

        #endregion

        #region 动画帧驱动

        float timer = 0; //计时timer
        readonly float fps = 30; //fps


        /// <summary>
        /// update
        /// </summary>
        /// <param name="delta"></param>
        public void _OnUpdate(float delta)
        {
            if (isPauseAnimation)
                return;


            //这里需要注意,Play接口操作的都共用的底层状态,
            //为了防止Update执行一半状态变了
            //所以在update开始进行状态比对,
            //如果需要播放新动画,则设置好状态
            if (nextPlayAniClip != null)
            {
                //播放
                Play(nextPlayAniClip, nextPlaySubAniClip, nextPlayCrosstimer);

                nextPlayAniClip = null;
                nextPlaySubAniClip = null;

                return;
            }


            //下面都是推帧逻辑
            if (lastClip == null)
                return;

            timer += delta;


            //判断是否在动画切换之间的过渡
            if (_crossTimer >= 0)
            {
                _crossTimer -= delta;

                if (_crossTimer <= 0)
                {
                    //过渡结束 timer 归零
                    timer = 0;
                    crossFrame = null;
                }
            }

            //这里要用一个稳定的fps，就用播放的第一个动画的fps作为稳定fps
            int _frameCount = (int) (timer * fps);
            //
            if (_frameCount == frameCounter)
            {
                return;
            }

            //增加一个限制，不准动画跳帧
            if (_frameCount > frameCounter + 1)
            {
                _frameCount = frameCounter + 1;
                timer = _frameCount / fps;
            }

            frameCounter = _frameCount;

            //1.不需要过渡,直接播放动画
            if (_crossTimer <= 0)
            {
                //只有播放动作才推帧
                int frame = lastframe + 1;
                if (frame > endframe)
                {
                    if (bLooped)
                    {
                        frame = startframe;
                    }
                    else
                    {
                        frame = endframe;
//
                        //播放默认动画
                        if (!string.IsNullOrEmpty(this.defaultAnimationName))
                        {
                            this.Play(defaultAnimationName);
                        }
                    }
                }

                int frameCache = lastframe;

                //设置动画
                SetPose(lastClip, frame, true);

                lastframe = frame;

                //判断动画是否正好结束
                if (frameCache < endframe && frame == endframe)
                {
                    if (CurClip != null)
                    {
                        this.TriggerAnimationState(CurClip.name, AnimationState.OnPlayEnd);
                    }
                }
            }

            //2.计算过渡插帧 播放
            else if (_crossTimer > 0)
            {
                if (crossFrame != null)
                {
                    //两帧之间的距离进度
                    float l = 1.0f - _crossTimer / _crossTimerTotal;

                    SetPoseLerp(crossFrame, lastClip.frames[startframe], l);
                }
            }

            //触发帧驱动
            TriggerAnimationState(CurClip.name, AnimationState.OnUpdate);
        }

        int transcode = -1;

        /// <summary>
        /// 当前动画的骨骼节点
        /// </summary>
        Transform[] curBoneTrans = null;

        /// <summary>
        /// 所有Transform的缓存
        /// </summary>
        Dictionary<string, Transform> transformCacheMap = new Dictionary<string, Transform>();

        private int lastSetClipHash = -1;
        private int lastSetFrame = -1;

        /// <summary>
        /// 每一帧的设置动作
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="frame"></param>
        /// <param name="reset"></param>
        /// <param name="parent"></param>
        public void SetPose(AniClip clip, int frame, bool reset = false, Transform parent = null)
        {
            if (clip == null)
            {
                int i = 0;
            }

            //这里防止外部循环设置同一帧数据
            if (clip.GetHashCode() == lastSetClipHash && frame == lastSetFrame)
            {
                return;
            }

            lastSetClipHash = clip.GetHashCode();
            lastSetFrame = frame;

            if (clip.bonehash != transcode)
            {
                curBoneTrans = new Transform[clip.boneinfo.Count];
                for (int i = 0; i < clip.boneinfo.Count; i++)
                {
                    var name = clip.boneinfo[i];
                    Transform t = null;
                    if (!transformCacheMap.TryGetValue(name, out t))
                    {
                        t = this.transform.Find(name);
                        transformCacheMap[name] = t;
                    }

                    curBoneTrans[i] = t;
                }

                transcode = clip.bonehash;
            }

            bool badd = false;

            if (lastClip == clip && !reset)
            {
                if (lastframe + 1 == frame) badd = transform;
                if (clip.loop && lastframe == clip.frames.Count - 1 && frame == 0)
                    badd = true;
            }

            for (int i = 0; i < curBoneTrans.Length; i++)
            {
                if (curBoneTrans[i] == null) continue;
                if (parent != null && parent != curBoneTrans[i])
                {
                    if (curBoneTrans[i].IsChildOf(parent) == false) continue;
                }

                clip.frames[frame].bonesinfo[i].UpdateTran(curBoneTrans[i], badd);
            }

            if (clip.frames.Count > 0 && frame >= 0)
            {
                SetBoxColiderAttribute(clip.frames[frame]); //设置碰撞盒
                if (isShowBoxLine)
                {
                    SetDebugDot(clip.frames[frame]); //设置触发点
                }

                if (isPlayRunTime)
                {
                    SetEffect(clip.frames[frame]); //设置/检测特效
                    SetAudio(clip.frames[frame]);
                }
            }
        }


        /// <summary>
        /// 插值设置动作
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="lerp"></param>
        public void SetPoseLerp(Frame src, Frame dest, float lerp)
        {
            //以骨骼数据为主，进行播放，没记录的则不播放
            var count = Mathf.Min(curBoneTrans.Length, dest.bonesinfo.Count);

            for (int i = 0; i < count; i++)
            {
                src.bonesinfo[i].UpdateTranLerp(curBoneTrans[i], dest.bonesinfo[i], lerp);
            }

            SetBoxColiderAttribute(dest); //设置碰撞盒

            if (isShowBoxLine)
            {
                SetDebugDot(dest); //设置触发点
            }

            if (isPlayRunTime)
            {
                SetEffect(dest); //设置/检测特效

                SetAudio(dest);
            }
        }


        //EffectMng effectmng = new EffectMng();
        bool isAutoUpdate = true;

        public void SkipAutoUpdate()
        {
            isAutoUpdate = false;
        }

        public void UnSkipAutoUpdate()
        {
            isAutoUpdate = true;
        }


        private void FixedUpdate()
        {
            if (isPlayRunTime && isAutoUpdate)
            {
                _OnUpdate(Time.deltaTime);
            }

            if (ischange != isShowBoxLine)
            {
                CheckShowBox();
            }
        }

        #endregion


        #region  帧碰撞盒

        bool ischange = false;

        void CheckShowBox()
        {
            ischange = isShowBoxLine;
            if (isShowBoxLine)
            {
                foreach (var o in mBoxList)
                {
                    if (!o.GetComponent<Collider_Vis>())
                        o.AddComponent<Collider_Vis>();
                    if (!o.GetComponent<LineRenderer>())
                        o.AddComponent<LineRenderer>();
                    if (!o.GetComponent<MeshRenderer>())
                        o.AddComponent<MeshRenderer>();
                    SetBoxColor(o);
                }

                foreach (var o in mDotList)
                {
                    if (!o.GetComponent<Point_Vis>())
                        o.AddComponent<Point_Vis>();
                    if (!o.GetComponent<LineRenderer>())
                        o.AddComponent<LineRenderer>();
                }

                //o.GetComponent<Collider_Vis>().updateColl();
            }
            else
            {
                foreach (var o in mBoxList)
                {
                    if (o.GetComponent<Collider_Vis>())
                        DestroyImmediate(o.GetComponent<Collider_Vis>());
                    if (o.GetComponent<LineRenderer>())
                        DestroyImmediate(o.GetComponent<LineRenderer>());
                    if (o.GetComponent<MeshRenderer>())
                        DestroyImmediate(o.GetComponent<MeshRenderer>());
                }

                foreach (var o in mDotList)
                {
                    if (o.GetComponent<Point_Vis>())
                        DestroyImmediate(o.GetComponent<Point_Vis>());
                    if (o.GetComponent<LineRenderer>())
                        DestroyImmediate(o.GetComponent<LineRenderer>());
                }
            }
        }

        void SetBoxColiderAttribute(Frame src)
        {
            if (_boxes != null)
            {
                _boxes.transform.localPosition = Vector3.zero;
                _boxes.transform.localRotation = new Quaternion(0, 0, 0, 0);
            }

            if (src.boxesinfo != null)
            {
                //剔除null
                for (int i = mBoxList.Count - 1; i >= 0; i--)
                {
                    if (mBoxList[i] == null)
                    {
                        mBoxList.RemoveAt(i);
                    }
                }

                for (int i = 0; i < src.boxesinfo.Count; i++)
                {
                    if (mBoxList.Count - 1 < i)
                    {
                        CreateBox(1);
                    }

                    SetBoxAttribute(src.boxesinfo[i], mBoxList[i]);
                }

                if (mBoxList.Count > src.boxesinfo.Count)
                {
                    for (int i = src.boxesinfo.Count; i < mBoxList.Count; i++)
                    {
                        if (mBoxList[i].activeSelf)
                        {
                            mBoxList[i].SetActive(false);
                        }
                    }
                }
            }
        }

        //对象池
        List<GameObject> mBoxList = new List<GameObject>();
        List<List<GameObject>> mBoxArray = new List<List<GameObject>>();
        GameObject _boxes = null;

        [FormerlySerializedAs("IsShowBoxLine")]
        public bool isShowBoxLine = true;

        void CreateBox(int count)
        {
            if (!transform.Find("_boxes"))
            {
                _boxes = new GameObject("_boxes");
                _boxes.transform.parent = transform;
            }
            else
            {
                _boxes = transform.Find("_boxes").gameObject;
                if (mBoxList.Count == 0)
                {
                    foreach (Transform t in _boxes.transform)
                    {
                        if (t != null)
                        {
                            t.gameObject.SetActive(false);
                            mBoxList.Add(t.gameObject);
                        }
                    }

                    if (mBoxList.Count > 0)
                        return;
                }
            }

            //加载AttackBox
            for (int i = 0; i != count; i++)
            {
                AddBoxTo(_boxes);
            }
        }

        //添加box 
        void AddBoxTo(GameObject father)
        {
            GameObject o = null;
            o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.gameObject.name = "BoxColider";
            o.AddComponent<Collider_Vis>();
            var material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            material.color = new Color(1f, 1f, 1f, 0.2f);
            o.GetComponent<MeshRenderer>().material = material;

            o.transform.parent = father.transform;
            o.SetActive(false);
            o.hideFlags = HideFlags.DontSave;
            if (o != null)
            {
                mBoxList.Add(o);
            }
        }

        void SetBoxAttribute(AniBoxCollider _box, GameObject _curBox = null)
        {
            if (_curBox != null)
                _curBox.transform.localRotation = new Quaternion(0, 0, 0, 0);
            _curBox.SetActive(true);
            //重新设置box的属性
            _curBox.gameObject.name = _box.mName;
            _curBox.layer = LayerMask.NameToLayer(_box.mBoxType);
            //if (_curBox.transform.localPosition != _box.mPosition) 
            //{
            _curBox.transform.localPosition = _box.mPosition;
            //} 
            //计算scale
            var _colider = _curBox.GetComponent<BoxCollider>();
            _curBox.transform.localScale = new Vector3(_box.mSize.x / _colider.size.x, _box.mSize.y / _colider.size.y,
                _box.mSize.z / _colider.size.z);


            if (isShowBoxLine)
            {
                if (!_curBox.GetComponent<Collider_Vis>())
                    _curBox.AddComponent<Collider_Vis>();
                if (!_curBox.GetComponent<LineRenderer>())
                    _curBox.AddComponent<LineRenderer>();
                if (!_curBox.GetComponent<MeshRenderer>())
                    _curBox.AddComponent<MeshRenderer>();
                SetBoxColor(_curBox);

                //o.GetComponent<Collider_Vis>().updateColl();
            }
            else
            {
                if (_curBox.GetComponent<Collider_Vis>())
                    DestroyImmediate(_curBox.GetComponent<Collider_Vis>());
                if (_curBox.GetComponent<LineRenderer>())
                    DestroyImmediate(_curBox.GetComponent<LineRenderer>());
                if (_curBox.GetComponent<MeshRenderer>())
                    DestroyImmediate(_curBox.GetComponent<MeshRenderer>());
            }
        }

        public class BoxColor
        {
            public BoxColor(Color line, Color box)
            {
                linecolor = line;
                boxcolor = box;
            }

            public Color linecolor;
            public Color boxcolor;
        }

        public Dictionary<string, BoxColor> boxcolor = new Dictionary<string, BoxColor>()
        {
            {"box_attack", new BoxColor(Color.black, new Color(0f, 0f, 0f, 0.3f))},
            {"box_area", new BoxColor(Color.green, new Color(0f, 1f, 0f, 0.3f))},
            {"box_behurt", new BoxColor(Color.red, new Color(1f, 0f, 0f, 0.3f))}
        };

        public void SetBoxColor(GameObject _curBox)
        {
            if (isShowBoxLine)
            {
                //颜色
                Collider_Vis collider_Vis = null;
                LineRenderer lineRenderer = null;
                MeshRenderer meshRenderer = null;

                if (_curBox.GetComponent<MeshRenderer>() && _curBox.GetComponent<Collider_Vis>())
                {
                    collider_Vis = _curBox.GetComponent<Collider_Vis>();
                    meshRenderer = _curBox.GetComponent<MeshRenderer>();
                    collider_Vis.linewidth = 0.2f;
                    collider_Vis.updateColl();
                }

                lineRenderer = _curBox.GetComponent<LineRenderer>();

                var material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));

                if (boxcolor.ContainsKey(LayerMask.LayerToName(_curBox.layer))) //Attck
                {
                    if (collider_Vis != null)
                    {
                        collider_Vis.lineColor = boxcolor[LayerMask.LayerToName(_curBox.layer)].linecolor;
                    }

                    material.color = boxcolor[LayerMask.LayerToName(_curBox.layer)].boxcolor;
                }

                meshRenderer.enabled = true;
                lineRenderer.enabled = true;
                meshRenderer.material = material;
                collider_Vis.updateColl();
            }
        }

        #endregion

        #region 触发点

        List<GameObject> mDotList = new List<GameObject>();
        GameObject dot;

        public GameObject CreateDot()
        {
            if (transform.Find("_dotes"))
            {
                dot = transform.Find("_dotes").gameObject;
            }
            else
            {
                dot = new GameObject("_dotes");
            }

            dot.transform.parent = transform;
            dot.transform.localPosition = Vector3.zero;
            dot.transform.localRotation = new Quaternion(0, 0, 0, 0);
            dot.transform.localScale = Vector3.one;
            GameObject o = new GameObject();
            o.transform.localScale = new Vector3(3, 3, 3);
            o.AddComponent<Point_Vis>();
            o.GetComponent<Point_Vis>().UpdatePoint();
            o.transform.parent = dot.transform;
            mDotList.Add(o);
            return o;
        }

        public void SetDotAttribute(Dot d)
        {
            for (int i = mDotList.Count - 1; i >= 0; i--)
            {
                if (mDotList[i] == null)
                {
                    mDotList.RemoveAt(i);
                }
            }

            if (dot != null)
                dot.transform.localRotation = new Quaternion(0, 0, 0, 0);
            GameObject _curdot = mDotList.Find(o => !o.activeSelf);
            if (_curdot == null)
            {
                _curdot = CreateDot();
            }

            _curdot.SetActive(true);
            _curdot.transform.localPosition = d.position;
            _curdot.name = d.name;
            switch (d.name)
            {
                case "hold":
                    _curdot.GetComponent<Point_Vis>().lineColor = Color.black;
                    break;
                case "behold":
                    _curdot.GetComponent<Point_Vis>().lineColor = Color.red;
                    break;
                case "create":
                    _curdot.GetComponent<Point_Vis>().lineColor = Color.green;
                    break;
            }

            _curdot.GetComponent<Point_Vis>().UpdatePoint();
        }

        void ReturnDot()
        {
            for (int i = mDotList.Count - 1; i >= 0; i--)
            {
                if (mDotList[i] == null)
                {
                    mDotList.RemoveAt(i);
                }
            }

            foreach (var o in mDotList)
            {
                if (o != null)
                {
                    o.SetActive(false);
                }
            }
        }

        void SetDebugDot(Frame f)
        {
            ReturnDot(); //每一帧调用，先重置box
            if (f.boxesinfo != null)
            {
                foreach (var b in f.dotesinfo)
                {
                    SetDotAttribute(b);
                }
            }
        }

        #endregion

        #region 特效

        class EffectLife
        {
            public int frame = -1;
            public string name = "";
            public int effid;
            public int lifetime; //per pose -1;
        }

        List<EffectLife> effectLifeList = new List<EffectLife>();

        //每帧检测
        void SetEffect(Frame f)
        {
            UpdateEffect();

            foreach (var e in f.effectList)
            {
                //同一帧的永久特效，循环播放时候只加载一次
                if (bLooped && e.lifeframe == 0)
                {
                    var elife = effectLifeList.Find((el) => el.name == e.name);
                    //已经加载过了
                    if (elife != null)
                    {
                        continue;
                    }
                }

                //
                Transform o = this.transform.Find(e.follow);
                if (o != null)
                {
                    EffectLife d = new EffectLife();
                    d.lifetime = e.lifeframe;
                    d.effid = ResourceLoader.PlayEffectLooped(e.name, e.position, dir, o);
                    if (e.lifeframe == 0)
                    {
                        d.frame = lastframe;
                        d.name = e.name;
                    }

                    effectLifeList.Add(d);
                }
            }
        }

        //管理生命周期
        void UpdateEffect()
        {
            //编辑器中误操作，移除所有为null的引用
            for (int i = effectLifeList.Count - 1; i >= 0; i--)
            {
                if (effectLifeList[i].lifetime > 0) //等于0的是永久特效
                {
                    effectLifeList[i].lifetime--; //生命周期每帧 -1

                    if (effectLifeList[i].lifetime == 0) //生命周期结束 删除特效
                    {
                        ResourceLoader.CloseEffectLooped(effectLifeList[i].effid);
                        effectLifeList.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region  音效

        void SetAudio(Frame f)
        {
            foreach (var audio in f.aduioList)
            {
                ResourceLoader.PlaySoundOnce(audio);
            }
        }

        #endregion

        public int dir = 1;

        public int chardir
        {
            get { return dir; }
            set { dir = chardir; }
        }

        public void SetDir(int dir)
        {
            this.dir = dir;
            this.transform.LookAt(this.transform.position + new Vector3(dir == 1 ? 1 : -1, 0, 0), Vector3.up);
        }


        #region 事件监听回调

        public class AnimationStateCallback
        {
            public AnimationState State;
            public Action<int> Callback;
        }

        /// <summary>
        /// 所有监听的回调
        /// </summary>
        Dictionary<string, List<AnimationStateCallback>> onAnimationPlayEndCallbackDictionary =
            new Dictionary<string, List<AnimationStateCallback>>();


        /// <summary>
        /// 触发动画时间
        /// </summary>
        /// <param name="name"></param>
        /// <param name="state"></param>
        private void TriggerAnimationState(string name, AnimationState state)
        {
            //
            List<AnimationStateCallback> list = null;

            if (onAnimationPlayEndCallbackDictionary.TryGetValue(name, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var a = list[i];
                    if (a.State == state)
                    {
                        a.Callback(this.CurAniFrame);
                    }
                }
            }
        }

        /// <summary>
        /// 监听动画的状态
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        public void AddListener(string name, AnimationState state, Action<int> action)
        {
            if (name.Contains(".FBAni") == false)
            {
                name += ".FBAni";
            }

            List<AnimationStateCallback> list = null;

            if (onAnimationPlayEndCallbackDictionary.TryGetValue(name, out list))
            {
                list.Add(new AnimationStateCallback()
                {
                    State = state,
                    Callback = action,
                });
            }
            else
            {
                list = new List<AnimationStateCallback>();
                list.Add(new AnimationStateCallback()
                {
                    State = state,
                    Callback = action,
                });
                onAnimationPlayEndCallbackDictionary.Add(name, list);
            }
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        public void RemoveListener(string name, AnimationState state, Action<int> action)
        {
            if (name.Contains(".FBAni") == false)
            {
                name += ".FBAni";
            }

            List<AnimationStateCallback> list = null;

            if (onAnimationPlayEndCallbackDictionary.TryGetValue(name, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var a = list[i];
                    if (a.State == state && a.Callback == action)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        #endregion
    }
}