using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title シーン。SPACE で InGame へ、SHIFT でランキングを開閉する。
/// 画面そのものは <c>Assets/Prefabs/UI/TitleCanvas.prefab</c>(なまえ入力欄も中に入っている)で、
/// ここはそれを出してカメラと空気感を用意し、入力を捌くだけ。
/// </summary>
public class TitleController : MonoBehaviour
{
    [SerializeField] private string inGameSceneName = "InGame";
    [SerializeField] private Color background = new Color(0.045f, 0.035f, 0.07f);
    [SerializeField] private AudioTuning audioTuning = new AudioTuning();
    [SerializeField] private AtmosphereTuning atmosphere = new AtmosphereTuning();
    [SerializeField] private TouchTuning touch = new TouchTuning();

    private RankingView _ranking;
    private NameField _nameField;

    private void Awake()
    {
        // シーンのデシリアライズで Tuning が壊れていた場合の保険
        if (audioTuning == null) audioTuning = new AudioTuning();
        if (atmosphere == null) atmosphere = new AtmosphereTuning();
        if (touch == null || touch.buttonDiameter <= 0f) touch = new TouchTuning();
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

        if (atmosphere.enabled)
            new GameObject("AtmosphereFx").AddComponent<AtmosphereFx>().Init(cam, atmosphere);

        var canvas = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.titleCanvas : null);
        if (canvas != null)
        {
            _nameField = canvas.GetComponentInChildren<NameField>(true);
            _ranking = RankingView.Create(canvas.transform, "{S} : 閉じる      {C} : はじめる");
        }

        // スマホ用バーチャルパッド(タッチ環境でなければ隠れたまま)
        VirtualPad.Create(touch);
    }

    private void Update()
    {
        // ランキングを開いている間は名前入力を触れないようにしておく
        if (_nameField != null && _ranking != null)
        {
            bool show = !_ranking.IsOpen;
            if (_nameField.gameObject.activeSelf != show) _nameField.gameObject.SetActive(show);
        }

        if (InputHub.SpecialPressed && _ranking != null) { AudioManager.PlayUi(); _ranking.Toggle(); }
        if (InputHub.ConfirmPressed)
        {
            AudioManager.PlayUi();
            AudioManager.StartBgm();
            SceneManager.LoadScene(inGameSceneName);
        }
    }
}
