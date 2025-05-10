using UnityEngine;
using UnityEngine.Playables;

public class RalentizarTimeline : MonoBehaviour
{
    public PlayableDirector director;

    void Start()
    {
        if (director != null)
        {
            // Ralentiza todo el timeline a la mitad
            director.playableGraph.GetRootPlayable(0).SetSpeed(0.7f);
        }
    }
}
