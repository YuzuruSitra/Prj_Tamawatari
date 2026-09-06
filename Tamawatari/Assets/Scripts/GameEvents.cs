using UnityEngine;

/// <summary>
/// イベントが触れるゲーム側の参照一式。
/// 新しいイベントを足すときはここに必要な参照を増やす。
/// </summary>
public class GameEventContext
{
    public PlayerController Player;
    public Transform PlayerTransform;
    public PlatformSpawner PlatformSpawner;
    public EnemySpawner EnemySpawner;
    public AtmosphereFx Atmosphere;
    public UIManager Ui;
    public EventTuning Tuning;
}

/// <summary>
/// ランダムイベントの基底。継承して OnBegin / OnTick / OnEnd を書けば
/// EventDirector の抽選対象に加えられる。
/// </summary>
public abstract class GameEvent
{
    public abstract string DisplayName { get; }
    public virtual string Description => "";
    public virtual Color Tint => Color.white;

    /// <summary>0 より大きい値を返すと、その秒数を継続時間として使う(既定は durationRange 抽選)。</summary>
    public virtual float FixedDuration(GameEventContext ctx) => 0f;

    /// <summary>今このイベントを開始できるか(条件付きイベント用)。</summary>
    public virtual bool CanStart(GameEventContext ctx) => true;

    public virtual void OnBegin(GameEventContext ctx) { }
    public virtual void OnTick(GameEventContext ctx, float dt) { }
    public virtual void OnEnd(GameEventContext ctx) { }
}

/// <summary>
/// 磁気嵐:お化けが湧かなくなる代わりに、止まっていると左下へじわじわ流される。
/// </summary>
public class MagneticStormEvent : GameEvent
{
    public override string DisplayName => "磁気嵐  MAGNETIC STORM";
    public override string Description => "お化けは湧かない / 止まると流される";
    public override Color Tint => new Color(0.45f, 0.75f, 1f);

    public override void OnBegin(GameEventContext ctx)
    {
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnEnabled = false;
        if (ctx.Atmosphere != null) ctx.Atmosphere.SetWind(DriftDir(ctx) * 2.4f);
        EnemyController.FadeOutAll(ctx.Tuning != null ? ctx.Tuning.eventFadeTime : 0.6f);
    }

    public override void OnTick(GameEventContext ctx, float dt)
    {
        var p = ctx.Player;
        if (p == null) return;
        if (!p.IsGrounded) return;                       // 飛んでいる間は流されない

        Vector3 d = (Vector3)DriftDir(ctx);
        p.ExternalMove(d * (ctx.Tuning.stormDriftSpeed * dt));
    }

    public override void OnEnd(GameEventContext ctx)
    {
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnEnabled = true;
        if (ctx.Atmosphere != null) ctx.Atmosphere.SetWind(Vector2.zero);
    }

    private static Vector2 DriftDir(GameEventContext ctx)
    {
        Vector2 d = ctx.Tuning != null ? ctx.Tuning.stormDriftDir : new Vector2(-1f, -1f);
        return d.sqrMagnitude < 0.0001f ? new Vector2(-1f, -1f).normalized : d.normalized;
    }
}

/// <summary>
/// お化け大量発生:出現間隔が一気に縮まり、開始時にまとめて湧く。
/// </summary>
public class GhostSwarmEvent : GameEvent
{
    public override string DisplayName => "大量発生  GHOST SWARM";
    public override string Description => "お化けがどっと押し寄せる";
    public override Color Tint => new Color(1f, 0.45f, 0.4f);

    public override bool CanStart(GameEventContext ctx) => ctx.EnemySpawner != null;

    public override void OnBegin(GameEventContext ctx)
    {
        var es = ctx.EnemySpawner;
        if (es == null) return;
        es.SpawnEnabled = true;
        es.SpawnIntervalScale = Mathf.Max(0.05f, ctx.Tuning.swarmIntervalScale);
        es.SpawnBurst(Mathf.Max(0, ctx.Tuning.swarmBurstCount));
        CameraFollow.Instance?.ShakeHit();
    }

    public override void OnEnd(GameEventContext ctx)
    {
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnIntervalScale = 1f;
    }
}

/// <summary>
/// 色替え:いま自分が乗っている足場と「同じ色」の灯篭が消える(灰色になって踏めない)。
/// それ以外の色にはすべて乗れる。着地するたびに禁止色が更新されるので、色を変えながら渡っていく。
/// 足元の1つだけは色に関係なく常に安全。お化けは一度引くが、少しだけ湧き続ける。
/// </summary>
public class ColorRouletteEvent : GameEvent
{
    public override string DisplayName => "色替え  LANTERN ROULETTE";
    public override string Description => "いま乗っている色の灯篭が消える / 別の色へ跳べ";
    public override Color Tint => new Color(0.85f, 0.7f, 1f);

    private int _lastLand;
    private float _delay;

    public override bool CanStart(GameEventContext ctx) =>
        ctx.PlatformSpawner != null && ctx.PlatformSpawner.PaletteCount > 1;

    public override void OnBegin(GameEventContext ctx)
    {
        _lastLand = ctx.Player != null ? ctx.Player.LandCount : 0;
        _delay = ctx.Tuning != null ? ctx.Tuning.rouletteFirstSwitchDelay : 0.4f;

        // 一度は場を掃除するが、湧きは止めずに間隔だけ広げる(少しだけ湧く)
        EnemyController.FadeOutAll(ctx.Tuning != null ? ctx.Tuning.eventFadeTime : 0.7f);
        if (ctx.EnemySpawner != null)
        {
            ctx.EnemySpawner.SpawnEnabled = true;
            ctx.EnemySpawner.SpawnIntervalScale = Mathf.Max(0.1f, ctx.Tuning.rouletteIntervalScale);
        }
    }

    public override void OnTick(GameEventContext ctx, float dt)
    {
        var ps = ctx.PlatformSpawner;
        var p = ctx.Player;
        if (ps == null || p == null) return;

        if (_delay > 0f)
        {
            _delay -= dt;
            if (_delay <= 0f) Apply(ctx);
            return;
        }

        if (p.LandCount != _lastLand)      // 着地するたびに禁止色を更新
        {
            _lastLand = p.LandCount;
            Apply(ctx);
        }
    }

    public override void OnEnd(GameEventContext ctx)
    {
        ctx.PlatformSpawner?.SetBlockedColor(-1);
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnIntervalScale = 1f;
    }

    /// <summary>足元の色を禁止色にする(足元自体は例外として残す)。</summary>
    private static void Apply(GameEventContext ctx)
    {
        var here = ctx.Player != null ? ctx.Player.CurrentPlatform : null;
        if (here == null) { ctx.PlatformSpawner.SetBlockedColor(-1); return; }
        ctx.PlatformSpawner.SetBlockedColor(here.ColorId, here);
    }
}

/// <summary>
/// 濃霧:画面が霧に覆われ、自分のまわりのわずかな範囲しか見えなくなる。
/// 遠くの灯篭はにじむ明かりだけが頼りになる。
/// </summary>
public class DeepFogEvent : GameEvent
{
    public override string DisplayName => "濃霧  DEEP FOG";
    public override string Description => "灯りのそばしか見えない";
    public override Color Tint => new Color(0.62f, 0.72f, 0.86f);

    public override bool CanStart(GameEventContext ctx) => ctx.Ui != null;

    public override void OnBegin(GameEventContext ctx)
    {
        ctx.Ui?.SetFog(true);
        if (ctx.EnemySpawner != null)
            ctx.EnemySpawner.SpawnIntervalScale = Mathf.Max(0.1f, ctx.Tuning.fogIntervalScale);
        if (ctx.Atmosphere != null) ctx.Atmosphere.SetWind(new Vector2(0.6f, -0.2f));
    }

    public override void OnEnd(GameEventContext ctx)
    {
        ctx.Ui?.SetFog(false);
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnIntervalScale = 1f;
        if (ctx.Atmosphere != null) ctx.Atmosphere.SetWind(Vector2.zero);
    }
}

/// <summary>
/// 黄金の道(ボーナスタイム)。
/// お化けが一掃されて湧かなくなり、灯篭が全部金色に輝いて大きくなる。
/// どの色にも乗れて、着地するたびにボーナス点。ひたすら跳ねて稼ぐ爽快パート。
/// </summary>
public class GoldenPathEvent : GameEvent
{
    public override string DisplayName => "黄金の道  GOLDEN PATH";
    public override string Description => "お化けは消えた / 跳ぶたびにボーナス";
    public override Color Tint => new Color(1f, 0.82f, 0.32f);

    public override float FixedDuration(GameEventContext ctx) =>
        ctx.Tuning != null ? ctx.Tuning.goldenDuration : 0f;

    public override bool CanStart(GameEventContext ctx) => ctx.PlatformSpawner != null;

    private int _lastLand;

    public override void OnBegin(GameEventContext ctx)
    {
        _lastLand = ctx.Player != null ? ctx.Player.LandCount : 0;

        ctx.PlatformSpawner.SetBlockedColor(-1);
        ctx.PlatformSpawner.SetGolden(true, ctx.Tuning.goldenPlatformScale);

        EnemyController.FadeOutAll(ctx.Tuning != null ? ctx.Tuning.eventFadeTime : 0.7f);
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnEnabled = false;

        ctx.Ui?.SetWarmGlow(true);
        ctx.Atmosphere?.SetBoost(ctx.Tuning.goldenMoteBoost, new Color(1f, 0.82f, 0.38f, 0.7f));
        CameraFollow.Instance?.ShakeMerge();
    }

    public override void OnTick(GameEventContext ctx, float dt)
    {
        var p = ctx.Player;
        if (p == null || p.LandCount == _lastLand) return;

        _lastLand = p.LandCount;
        int add = GameManager.Instance != null ? GameManager.Instance.AddBonus(ctx.Tuning.goldenLandingBonus) : 0;
        ctx.Ui?.PopMessage($"BONUS  +{add}", new Color(1f, 0.86f, 0.4f));
        ShockwaveEffect.Spawn(p.transform.position, 2.2f, 0.4f, new Color(1f, 0.85f, 0.35f, 0.85f));
    }

    public override void OnEnd(GameEventContext ctx)
    {
        ctx.PlatformSpawner?.SetGolden(false, 1f);
        if (ctx.EnemySpawner != null) ctx.EnemySpawner.SpawnEnabled = true;
        ctx.Ui?.SetWarmGlow(false);
        ctx.Atmosphere?.SetBoost(1f, null);
    }
}
