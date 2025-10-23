using Prowl.PaperUI;
using Prowl.Quill;
using UnityEngine;

public class TestPaper : MonoBehaviour
{
    public WorldCanvasRenderer WorldCanvasRenderer;
    private Paper P;
    void Awake()
    {
        P = new Paper(new UnityCanvasRenderer(WorldCanvasRenderer), 100, 100, new FontAtlasSettings());
    }

    // Update is called once per frame
    void Update()
    {
        P.BeginFrame(Time.deltaTime);

        using(P.Column("main")
            .Size(P.Percent(100))
            .BackgroundColor(Prowl.Vector.Color.LightGray)
            .Enter())
        {
            P.Box("TitleBar")
                .Height(P.Percent(5))
                .Width(P.Percent(50))
                .BackgroundColor(Prowl.Vector.Color.Red);
        }

        P.EndFrame();   
    }
}
