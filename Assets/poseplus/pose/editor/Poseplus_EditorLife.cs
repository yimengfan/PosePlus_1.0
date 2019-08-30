using FB.PosePlus;

namespace Code.Game.poseplus.pose.editor
{
    //
    [UnityEditor.InitializeOnLoad]
    static public class Poseplus_EditorLife
    {
        static Poseplus_EditorLife()
        {
           AniPlayer.ResourceLoader = new NullIAniplayerForEditor();
        }
    }
}