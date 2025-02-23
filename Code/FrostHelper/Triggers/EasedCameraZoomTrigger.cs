﻿using Celeste.Mod.Entities;

namespace FrostHelper;

[CustomEntity("CCH/EasedCameraZoomTrigger",
              "FrostHelper/EasedCameraZoomTrigger")]
public class EasedCameraZoomTrigger : Trigger {
    // Properties set in Ahorn
    public Ease.Easer Easer;
    public float TargetZoom;
    public bool RevertOnLeave;
    public ZoomTriggerRevertMode RevertMode;
    public float EaseDuration;
    public bool FocusOnPlayer;
    public Vector2? FocusPoint;
    public bool DisableInPhotosensitiveMode;

    Vector2? prevFocusPoint;
    float initialZoom;
    bool prevFocusOnPlayer;

    public EasedCameraZoomTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Easer = data.Easing("easing", Ease.Linear);
        TargetZoom = data.Float("targetZoom", 2f);
        RevertOnLeave = data.Bool("revertOnLeave", false);
        EaseDuration = data.Float("easeDuration", 1f);
        FocusOnPlayer = data.Bool("focusOnPlayer", true);
        RevertMode = data.Enum("revertMode", ZoomTriggerRevertMode.RevertToNoZoom);
        DisableInPhotosensitiveMode = data.Bool("disableInPhotosensitiveMode", true);

        // A node can be used to specify the Y value of the focus point of the camera
        FocusPoint = data.FirstNodeNullable(offset);
    }

    Vector2? GetFocusPoint() {
        return FocusOnPlayer ? null : FocusPoint;
    }

    bool GetOnlyY() => !FocusOnPlayer;

    public override void Added(Scene scene) {
        base.Added(scene);
        ZoomManager.AddToSceneIfNeeded(scene);
    }

    public override void OnEnter(Player player) {
        initialZoom = RevertMode switch {
            ZoomTriggerRevertMode.RevertToNoZoom => 1f,
            ZoomTriggerRevertMode.RevertToPreviousZoom => ZoomManager.TargetZoom,
            _ => throw new NotImplementedException()
        };

        prevFocusPoint = ZoomManager.FocusPoint;
        prevFocusOnPlayer = ZoomManager.FocusZoomOnPlayer;
        ZoomManager.FocusZoomOnPlayer = FocusOnPlayer;
        if (!(DisableInPhotosensitiveMode && Settings.Instance.DisableFlashes)) {
            ZoomManager.DoZoom(Easer, TargetZoom, EaseDuration, GetFocusPoint(), GetOnlyY());
        }
    }

    public override void OnStay(Player player) {
        ZoomManager.FocusZoomOnPlayer = FocusOnPlayer;
    }

    public override void OnLeave(Player player) {
        if (RevertOnLeave) {
            ZoomManager.DoZoom(Easer, initialZoom, EaseDuration, prevFocusPoint, GetOnlyY());
            ZoomManager.FocusZoomOnPlayer = prevFocusOnPlayer;
        }
    }

    public Level Level => (Scene as Level)!;

    public ZoomManager ZoomManager => Scene.Tracker.GetEntity<ZoomManager>();
}

/// <summary>
/// An entity which controls zooming in and out the level's camera. There should only be one of these in a scene at once
/// </summary>
[Tracked]
public class ZoomManager : Entity {
    private ZoomManager() {
        Depth = Depths.Top; // Make sure that this is one of the last entities to get updated, to make sure that the camera focuses on the actual player position
    }

    /// <summary>
    /// A temporary value used to make sure only one <see cref="ZoomManager"/> is added to the scene at once.
    /// This is needed because <see cref="Scene.Add(Entity)"/> doesn't immediately add the entity to the tracker.
    /// Used exclusively by <see cref="AddToSceneIfNeeded(Scene)"/>, and gets reset by <see cref="Awake(Scene)"/>.
    /// </summary>
    static bool _justAddedAManager;
    public float TargetZoom = 1f;
    public bool FocusZoomOnPlayer;
    public Vector2? FocusPoint;
    public bool OnlyY;

    Coroutine zoomCoroutine;

    public static void AddToSceneIfNeeded(Scene scene) {
        if (!_justAddedAManager && scene.Tracker.GetEntity<ZoomManager>() == null) {
            _justAddedAManager = true;
            scene.Add(new ZoomManager());
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        _justAddedAManager = false;
        Level.ResetZoom();
    }

    public override void Update() {
        if (Level.Zoom == TargetZoom) {
            SetZoomFocusPoint(GetTargetZoomFocusPoint());
        }
        base.Update();
    }

    public void DoZoom(Ease.Easer easer, float zoom, float duration, Vector2? focusPoint, bool onlyY) {
        OnlyY = onlyY;
        if (zoom != TargetZoom) {
            StopZooming();
            Add(zoomCoroutine = new Coroutine(ZoomRoutine(easer, zoom, duration, focusPoint)));
        }
    }

    public void StopZooming() {
        if (zoomCoroutine != null) {
            Remove(zoomCoroutine);
        }
    }

    public IEnumerator ZoomRoutine(Ease.Easer easer, float zoom, float duration, Vector2? focusPoint) {
        FocusPoint = focusPoint;
        TargetZoom = zoom;
        float from = Level.Zoom;
        Vector2 prevFocus = Level.ZoomFocusPoint;
        for (float p = 0f; p < 1f; p += Engine.DeltaTime / duration) {
            float amt = easer(MathHelper.Clamp(p, 0f, 1f));
            Level.Zoom = Level.ZoomTarget = MathHelper.Lerp(from, zoom, amt);
            SetZoomFocusPoint(Vector2.Lerp(prevFocus, GetTargetZoomFocusPoint(), amt));
            yield return null;
        }
        Level.Zoom = Level.ZoomTarget = zoom;
        SetZoomFocusPoint(GetTargetZoomFocusPoint());
    }

    void SetZoomFocusPoint(Vector2 newPos) {
        if (OnlyY) {
            Level.ZoomFocusPoint.Y = newPos.Y;
        } else {
            Level.ZoomFocusPoint = newPos;
        }
    }

    public Vector2 GetTargetZoomFocusPoint() {
        if (TargetZoom == 1f) {
            return DefaultZoomFocusPoint;
        }

        if (FocusPoint.HasValue) {
            return FocusPoint.Value - Level.Camera.Position;
        }

        Player player;
        if (FocusZoomOnPlayer && (player = Scene.Tracker.GetEntity<Player>()) is not null) {
            return player.Position - Level.Camera.Position;
        }

        return DefaultZoomFocusPoint;
    }

    public Level Level => (Scene as Level)!;

    public static Vector2 DefaultZoomFocusPoint = new Vector2(320f, 180f) / 2f;
}

public enum ZoomTriggerRevertMode {
    RevertToNoZoom,
    RevertToPreviousZoom,
}
