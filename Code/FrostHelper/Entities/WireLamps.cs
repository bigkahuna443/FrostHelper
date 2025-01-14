﻿namespace FrostHelper;

/// <summary>
/// Wire Lamps from A Christmas Night
/// </summary>
[CustomEntity("FrostHelper/WireLamps")]
public class WireLamps : Entity {
    public MTexture LampTexture;

    public WireLamps(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Vector2 to = data.Nodes[0] + offset;
        Curve = new SimpleCurve(Position, to, Vector2.Zero);
        Depth = (data.Bool("above", false) ? -8500 : 2000) - 1;
        Random random = new Random((int) Math.Min(Position.X, to.X));
        Color[] colors = data.GetColors("colors", defaultColors);
        Color = data.GetColor("wireColor", "595866");
        sineX = random.NextFloat(4f);
        sineY = random.NextFloat(4f);

        lights = new VertexLight[data.Int("lightCount", 3)];
        var lightAlpha = data.Float("lightAlpha", 1f);
        var lightStartFade = data.Int("lightStartFade", 8);
        var lightEndFade = data.Int("lightEndFade", 16);

        for (int i = 0; i < lights.Length; i++) {
            lights[i] = new VertexLight(colors[random.Next(0, colors.Length)], lightAlpha, lightStartFade, lightEndFade);
            Add(lights[i]);
        }

        LampTexture = GFX.Game[data.Attr("lampSprite", "objects/FrostHelper/wireLamp")];
    }

    static Color[] defaultColors = new Color[]
    {
        Color.Red,
        Color.Yellow,
        Color.Blue,
        Color.Green,
        Color.Orange
    };

    public override void Render() {
        Level level = SceneAs<Level>();
        Vector2 value = new Vector2((float) Math.Sin(sineX + level.WindSineTimer * 2f), (float) Math.Sin(sineY + level.WindSineTimer * 2.8f)) * 8f * level.VisualWind;
        Curve.Control = (Curve.Begin + Curve.End) / 2f + new Vector2(0f, 24f) + value;
        Vector2 start = Curve.Begin;

        for (int i = 1; i <= 16; i++) {
            float percent = i / 16f;
            Vector2 point = Curve.GetPoint(percent);
            Draw.Line(start, point, Color);
            start = point;
        }

        for (int i = 1; i <= lights.Length; i++) {
            float percent = i / (lights.Length + 1f);
            Vector2 point = Curve.GetPoint(percent);
            LampTexture.DrawCentered(point, getColor(i));
            lights[i - 1].Position = point - Position;
        }

        base.Render();
    }

    public VertexLight[] lights;

    public Color Color;

    public SimpleCurve Curve;

    private float sineX;

    private float sineY;

    Color getColor(int i) {
        return lights[(i - 1) % lights.Length].Color;
    }
}
