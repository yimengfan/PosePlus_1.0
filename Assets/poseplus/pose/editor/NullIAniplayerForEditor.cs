using UnityEngine;

namespace FB.PosePlus
{
    public class NullIAniplayerForEditor :  IAniplayerResourceLoader
    {
        public void PlayEffect(string name, Vector3 pos, int dir)
        {
          
        }

        public void PlayEffect(string name, Transform follow, Vector3 pos, bool isfollow, int dir)
        {
          
        }

        public int PlayEffectLooped(string name, Vector3 pos, int dir = -1, Transform follow = null)
        {
            return 1;
        }

        public void CloseEffectLooped(int effid)
        {
           
        }

        public void PlaySoundOnce(string name)
        {
          
        }

        public void CleanAllEffect()
        {
            
        }
    }
}