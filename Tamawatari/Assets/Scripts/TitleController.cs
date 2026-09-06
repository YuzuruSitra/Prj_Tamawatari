using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title シーン。SPACE で InGame へ、SHIFT でランキングを開閉する。
/// InGame と同じハロウィン「灯篭の道」トーンで組む。
/// </summary>
public class TitleController : MonoBehaviour
{
    [SerializeField] private string inGameSceneName = "InGame";
    [SerializeField] private Color background = new Color(0.045f, 0.035f, 0.07f);
    [SerializeField] private FontTuning fonts = new FontTuning();
    [SerializeField] private AudioTuning audioTuning = new AudioTuning();
    [SerializeField] private AtmosphereTuning atmosphere = new AtmosphereTuning();

    private RankingView _ranking;

    private void Awake()
    {
        if (fonts == null) fonts = new FontTuning();
        MockUtil.ConfigureFonts(fonts.display, fonts.body, fonts.displayResource, fonts.bodyResource);
        if (audioTuning == null) audioTuning = new AudioTuning();
        AudioManager.Tuning = audioTuning;

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.transform.rotation = Quaternion.identity;

        if (atmosphere != null && atmosphere.enabled)
            new GameObject("AtmosphereFx").AddComponent<AtmosphereFx>().Init(cam, atmosphere);

        BuildUI();
    }

    private void BuildUI()
    {
        var root = MockUtil.CreateCanvas("TitleCanvas").transform;

        MockUtil.CreateImage(root, new Color(0.02f, 0.01f, 0.04f, 0.9f),
            Vector2.zero, Vector2.one, new Vector2(-140, -140), new Vector2(140, 140),
            "Vignette", MockUtil.VignetteSprite);

        // 灯篭が並ぶ道
        for (int i = 0; i < 7; i++)
        {
            float x = -540 + i * 180f;
            float y = -190 + Mathf.Abs(i - 3) * 14f;
            MockUtil.CreateBox(root, new Color(1f, 0.6f, 0.22f, 0.26f), new Vector2(x, y), new Vector2(230, 230),
                "lampGlow", sprite: MockUtil.GlowSprite);
            MockUtil.CreateBox(root, new Color(0.2f, 0.16f, 0.23f), new Vector2(x, y), new Vector2(46, 46), "lamp", circle: true);
            MockUtil.CreateBox(root, new Color(1f, 0.72f, 0.3f), new Vector2(x, y), new Vector2(24, 24), "lampCore", circle: true);
        }

        MockUtil.CreateText(root, "T A M A W A T A R I", 68, TextAnchor.MiddleCenter,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 70), new Vector2(0, 190),
            new Color(1f, 0.78f, 0.34f), display: true);
        MockUtil.CreateText(root, "灯篭の道をわたる", 24, TextAnchor.MiddleCenter,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 24), new Vector2(0, 64),
            new Color(0.82f, 0.76f, 0.72f));
        InputLabel.Bind(MockUtil.CreateText(root, "", 27, TextAnchor.MiddleCenter,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -84), new Vector2(0, -34),
            new Color(0.94f, 0.90f, 0.83f)), "{C} : はじめる      {S} : ランキング");

        InputLabel.Bind(MockUtil.CreateText(root, "", 19, TextAnchor.LowerCenter,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 30), new Vector2(0, 92),
            new Color(0.65f, 0.62f, 0.62f)),
            "{C}: 狙う / 溜める・離してジャンプ    {S}: 溜めをキャンセルして半分の衝撃    魂3つ + {S}長押し: 統合\n" +
            "足場のど真ん中に降りると PERFECT。敵を連続で倒すと KILL 連鎖が加熱してスコアが跳ね上がる");

        _ranking = RankingView.Create(root, "{S} : 閉じる      {C} : はじめる");
    }

    private void Update()
    {
        if (InputHub.SpecialPressed) { AudioManager.PlayUi(); _ranking.Toggle(); }
        if (InputHub.ConfirmPressed)
        {
            AudioManager.PlayUi();
            AudioManager.StartBgm();
            SceneManager.LoadScene(inGameSceneName);
        }
    }
}
