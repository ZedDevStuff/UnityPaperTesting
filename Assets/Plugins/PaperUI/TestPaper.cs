using Prowl.PaperUI;
using Prowl.Quill;
using UnityEngine;

public class TestPaper : MonoBehaviour
{
    private Paper P;
    void Awake()
    {
        P = new Paper(new UnityCanvasRenderer(), 100, 100, new FontAtlasSettings());
    }

    // Update is called once per frame
    void Update()
    {
        P.BeginFrame(Time.deltaTime);

        P.Box("box")
            .Size(P.Percent(100))
            .BackgroundColor(Prowl.Vector.Color.Blue);

        P.EndFrame();   
    }
}
