using UnityEngine;

namespace FB.PosePlus
{
    public interface IAniplayerResourceLoader
    {
        void PlayEffect(string name, Vector3 pos, int dir);

        void PlayEffect(string name, Transform follow, Vector3 pos, bool isfollow, int dir);

        int PlayEffectLooped(string name, Vector3 pos, int dir = -1, Transform follow = null);
        void CloseEffectLooped(int effid);

        void PlaySoundOnce(string name);

        void CleanAllEffect();
    }
}