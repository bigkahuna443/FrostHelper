﻿using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace FrostHelper
{
    // State 9 is vanilla Dream Block Dash
    //[CustomEntity("FrostHelper/CustomDreamBlock")]
    [Tracked]
    public class CustomDreamBlock : Solid
    {
        public CustomDreamBlock(EntityData data, Vector2 offset) : base(data.Position + offset, (float)data.Width, (float)data.Height, true)
        {
            whiteFill = 0f;
            whiteHeight = 1f;
            wobbleFrom = Calc.Random.NextFloat(6.28318548f);
            wobbleTo = Calc.Random.NextFloat(6.28318548f);
            wobbleEase = 0f;
            Depth = -11000;
            node = data.FirstNodeNullable(new Vector2?(offset));
            fastMoving = data.Bool("fastMoving", false);
            oneUse = data.Bool("oneUse", false);
            if (data.Bool("below", false))
            {
                Depth = 5000;
            }
            SurfaceSoundIndex = 11;
            particleTextures = new MTexture[]
            {
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null)
            };
            activeBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
            disabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
            activeLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
            disabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));
            DashSpeed = data.Float("speed", 240f);
            AllowRedirects = data.Bool("allowRedirects");
            AllowRedirectsInSameDir = data.Bool("allowSameDirectionDash");
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f);
        }

        public float DashSpeed;
        public bool AllowRedirects;
        public bool AllowRedirectsInSameDir;
        public float SameDirectionSpeedMultiplier;

        public override void Added(Scene scene)
        {
            base.Added(scene);
            playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
            // Handle moving
            if (playerHasDreamDash && node != null)
            {
                Vector2 start = Position;
                Vector2 end = node.Value;
                float num = Vector2.Distance(start, end) / 12f;
                bool flag2 = fastMoving;
                if (flag2)
                {
                    num /= 3f;
                }
                Tween tween = Tween.Create(Tween.TweenMode.YoyoLooping, Ease.SineInOut, num, true);
                tween.OnUpdate = delegate (Tween t)
                {
                    bool collidable = Collidable;
                    if (collidable)
                    {
                        MoveTo(Vector2.Lerp(start, end, t.Eased));
                    }
                    else
                    {
                        MoveToNaive(Vector2.Lerp(start, end, t.Eased));
                    }
                };
                Add(tween);
            }
            if (!playerHasDreamDash)
            {
                Add(occlude = new LightOcclude(1f));
            }
            Setup();
        }

        public void Setup()
        {
            particles = new DreamParticle[(int)(Width / 8f * (Height / 8f) * 0.7f)];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(Width), Calc.Random.NextFloat(Height));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();
                particles[i].Color = Color.LightGray * (0.5f + (float)particles[i].Layer / 2f * 0.5f);
                bool flag = playerHasDreamDash;
                if (flag)
                {
                    switch (particles[i].Layer)
                    {
                        case 0:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"));
                            break;
                        case 1:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"));
                            break;
                        case 2:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"));
                            break;
                    }
                }
            }
        }

        public void OnPlayerExit(Player player)
        {
            Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
            Vector2 value = Vector2.Zero;
            bool flag = CollideCheck(player, Position + Vector2.UnitX * 4f);
            if (flag)
            {
                value = Vector2.UnitX;
            }
            else
            {
                bool flag2 = CollideCheck(player, Position - Vector2.UnitX * 4f);
                if (flag2)
                {
                    value = -Vector2.UnitX;
                }
                else
                {
                    bool flag3 = CollideCheck(player, Position + Vector2.UnitY * 4f);
                    if (flag3)
                    {
                        value = Vector2.UnitY;
                    }
                    else
                    {
                        bool flag4 = CollideCheck(player, Position - Vector2.UnitY * 4f);
                        if (flag4)
                        {
                            value = -Vector2.UnitY;
                        }
                    }
                }
            }
            bool flag5 = value != Vector2.Zero;
            if (flag5)
            {
            }
            bool flag6 = oneUse;
            if (flag6)
            {
                OneUseDestroy();
            }
        }

        private void OneUseDestroy()
        {
            Collidable = Visible = false;
            DisableStaticMovers();
            RemoveSelf();
        }

        public override void Update()
        {
            base.Update();
            bool flag = playerHasDreamDash;
            if (flag)
            {
                animTimer += 6f * Engine.DeltaTime;
                wobbleEase += Engine.DeltaTime * 2f;
                bool flag2 = wobbleEase > 1f;
                if (flag2)
                {
                    wobbleEase = 0f;
                    wobbleFrom = wobbleTo;
                    wobbleTo = Calc.Random.NextFloat(6.28318548f);
                }
                SurfaceSoundIndex = 12;
            }
        }

        public bool BlockedCheck()
        {
            TheoCrystal theoCrystal = CollideFirst<TheoCrystal>();
            bool flag = theoCrystal != null && !TryActorWiggleUp(theoCrystal);
            bool result;
            if (flag)
            {
                result = true;
            }
            else
            {
                Player player = CollideFirst<Player>();
                bool flag2 = player != null && !TryActorWiggleUp(player);
                result = flag2;
            }
            return result;
        }

        private bool TryActorWiggleUp(Entity actor)
        {
            bool collidable = Collidable;
            Collidable = true;
            for (int i = 1; i <= 4; i++)
            {
                bool flag = !actor.CollideCheck<Solid>(actor.Position - Vector2.UnitY * (float)i);
                if (flag)
                {
                    actor.Position -= Vector2.UnitY * (float)i;
                    Collidable = collidable;
                    return true;
                }
            }
            Collidable = collidable;
            return false;
        }

        public override void Render()
        {
            Camera camera = SceneAs<Level>().Camera;
            bool flag = Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom;
            if (!flag)
            {
                Draw.Rect(shake.X + X, shake.Y + Y, Width, Height, playerHasDreamDash ? activeBackColor : disabledBackColor);
                Vector2 position = SceneAs<Level>().Camera.Position;
                for (int i = 0; i < particles.Length; i++)
                {
                    int layer = particles[i].Layer;
                    Vector2 vector = particles[i].Position;
                    vector += position * (0.3f + 0.25f * (float)layer);
                    vector = PutInside(vector);
                    Color color = particles[i].Color;
                    bool flag2 = layer == 0;
                    MTexture mtexture;
                    if (flag2)
                    {
                        int num = (int)((particles[i].TimeOffset * 4f + animTimer) % 4f);
                        mtexture = particleTextures[3 - num];
                    }
                    else
                    {
                        bool flag3 = layer == 1;
                        if (flag3)
                        {
                            int num2 = (int)((particles[i].TimeOffset * 2f + animTimer) % 2f);
                            mtexture = particleTextures[1 + num2];
                        }
                        else
                        {
                            mtexture = particleTextures[2];
                        }
                    }
                    bool flag4 = vector.X >= X + 2f && vector.Y >= Y + 2f && vector.X < Right - 2f && vector.Y < Bottom - 2f;
                    if (flag4)
                    {
                        mtexture.DrawCentered(vector + shake, color);
                    }
                }
                bool flag5 = whiteFill > 0f;
                if (flag5)
                {
                    Draw.Rect(X + shake.X, Y + shake.Y, Width, Height * whiteHeight, Color.White * whiteFill);
                }
                WobbleLine(shake + new Vector2(X, Y), shake + new Vector2(X + Width, Y), 0f);
                WobbleLine(shake + new Vector2(X + Width, Y), shake + new Vector2(X + Width, Y + Height), 0.7f);
                WobbleLine(shake + new Vector2(X + Width, Y + Height), shake + new Vector2(X, Y + Height), 1.5f);
                WobbleLine(shake + new Vector2(X, Y + Height), shake + new Vector2(X, Y), 2.5f);
                Draw.Rect(shake + new Vector2(X, Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
                Draw.Rect(shake + new Vector2(X + Width - 2f, Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
                Draw.Rect(shake + new Vector2(X, Y + Height - 2f), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
                Draw.Rect(shake + new Vector2(X + Width - 2f, Y + Height - 2f), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
            }
        }

        private Vector2 PutInside(Vector2 pos)
        {
            while (pos.X < X)
            {
                pos.X += Width;
            }
            while (pos.X > X + Width)
            {
                pos.X -= Width;
            }
            while (pos.Y < Y)
            {
                pos.Y += Height;
            }
            while (pos.Y > Y + Height)
            {
                pos.Y -= Height;
            }
            return pos;
        }

        private void WobbleLine(Vector2 from, Vector2 to, float offset)
        {
            float num = (to - from).Length();
            Vector2 vector = Vector2.Normalize(to - from);
            Vector2 vector2 = new Vector2(vector.Y, -vector.X);
            Color color = playerHasDreamDash ? activeLineColor : disabledLineColor;
            Color color2 = playerHasDreamDash ? activeBackColor : disabledBackColor;
            bool flag = whiteFill > 0f;
            if (flag)
            {
                color = Color.Lerp(color, Color.White, whiteFill);
                color2 = Color.Lerp(color2, Color.White, whiteFill);
            }
            float scaleFactor = 0f;
            int num2 = 16;
            int num3 = 2;
            while ((float)num3 < num - 2f)
            {
                float num4 = Lerp(LineAmplitude(wobbleFrom + offset, (float)num3), LineAmplitude(wobbleTo + offset, (float)num3), wobbleEase);
                bool flag2 = (float)(num3 + num2) >= num;
                if (flag2)
                {
                    num4 = 0f;
                }
                float num5 = Math.Min((float)num2, num - 2f - (float)num3);
                Vector2 vector3 = from + vector * (float)num3 + vector2 * scaleFactor;
                Vector2 vector4 = from + vector * ((float)num3 + num5) + vector2 * num4;
                Draw.Line(vector3 - vector2, vector4 - vector2, color2);
                Draw.Line(vector3 - vector2 * 2f, vector4 - vector2 * 2f, color2);
                Draw.Line(vector3, vector4, color);
                scaleFactor = num4;
                num3 += num2;
            }
        }

        private float LineAmplitude(float seed, float index)
        {
            return (float)(Math.Sin((double)(seed + index / 16f) + Math.Sin((double)(seed * 2f + index / 32f)) * 6.2831854820251465) + 1.0) * 1.5f;
        }

        private float Lerp(float a, float b, float percent)
        {
            return a + (b - a) * percent;
        }

        public IEnumerator Activate()
        {
            Level level = SceneAs<Level>();
            yield return 1f;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            Add(shaker = new Shaker(true, delegate (Vector2 t)
            {
                shake = t;
            }));
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime)
            {
                whiteFill = Ease.CubeIn(p);
                yield return null;
            }
            shaker.On = false;
            yield return 0.5f;
            ActivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            for (float p2 = 1f; p2 > 0f; p2 -= Engine.DeltaTime * 0.5f)
            {
                whiteHeight = p2;
                bool flag = level.OnInterval(0.1f);
                if (flag)
                {
                    int i = 0;
                    while ((float)i < Width)
                    {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + (float)i, Y + Height * whiteHeight + 1f));
                        i += 4;
                    }
                }
                bool flag2 = level.OnInterval(0.1f);
                if (flag2)
                {
                    level.Shake(0.3f);
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            while (whiteFill > 0f)
            {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
            yield break;
        }

        public void ActivateNoRoutine()
        {
            bool flag = !playerHasDreamDash;
            if (flag)
            {
                playerHasDreamDash = true;
                Setup();
                Remove(occlude);
                whiteHeight = 0f;
                whiteFill = 0f;
                bool flag2 = shaker != null;
                if (flag2)
                {
                    shaker.On = false;
                }
            }
        }

        public void FootstepRipple(Vector2 position)
        {
            bool flag = playerHasDreamDash;
            if (flag)
            {
                DisplacementRenderer.Burst burst = (Scene as Level).Displacement.AddBurst(position, 0.5f, 0f, 40f, 1f, null, null);
                burst.WorldClipCollider = Collider;
                burst.WorldClipPadding = 1;
            }
        }

        private Color activeBackColor;

        private Color disabledBackColor;

        private Color activeLineColor;

        private Color disabledLineColor;

        private bool playerHasDreamDash;

        private Vector2? node;

        private LightOcclude occlude;

        private MTexture[] particleTextures;

        private DreamParticle[] particles;

        private float whiteFill;

        private float whiteHeight;

        private Vector2 shake;

        private float animTimer;

        private Shaker shaker;

        private bool fastMoving;

        private bool oneUse;

        private float wobbleFrom;

        private float wobbleTo;

        private float wobbleEase;

        private struct DreamParticle
        {
            public Vector2 Position;

            public int Layer;

            public Color Color;

            public float TimeOffset;
        }

        // CUSTOM DREAM DASH STATE
        public static int DreamDashUpdate()
        {
            Player player = FrostModule.StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Vector2 position = player.Position;
            player.Speed = player.DashDir * data.Get<CustomDreamBlock>("customDreamBlock").DashSpeed;
            player.NaiveMove(player.Speed * Engine.DeltaTime);
            float dreamDashCanEndTimer = data.Get<float>("dreamDashCanEndTimer");
            bool flag = dreamDashCanEndTimer > 0f;
            if (flag)
            {
                data.Set<float>("dreamDashCanEndTimer", dreamDashCanEndTimer -= Engine.DeltaTime);
            }
            CustomDreamBlock dreamBlock = player.CollideFirst<CustomDreamBlock>();
            if (dreamBlock == null)
            {
                if (DreamDashedIntoSolid(player))
                {
                    bool invincible = SaveData.Instance.Assists.Invincible;
                    if (invincible)
                    {
                        player.Position = position;
                        player.Speed *= -1f;
                        player.Play("event:/game/general/assist_dreamblockbounce", null, 0f);
                    }
                    else
                    {
                        player.Die(Vector2.Zero, false, true);
                    }
                }
                else
                {
                    if (dreamDashCanEndTimer <= 0f)
                    {
                        Celeste.Celeste.Freeze(0.05f);
                        bool flag5 = Input.Jump.Pressed && player.DashDir.X != 0f;
                        if (flag5)
                        {
                            data.Set("dreamJump", true);
                            player.Jump(true, true);
                        }
                        else
                        {
                            if (player.DashDir.Y >= 0f || player.DashDir.X != 0f)
                            {
                                bool flag7 = player.DashDir.X > 0f && player.CollideCheck<Solid>(player.Position - Vector2.UnitX * 5f);
                                if (flag7)
                                {
                                    player.MoveHExact(-5, null, null);
                                }
                                else
                                {
                                    bool flag8 = player.DashDir.X < 0f && player.CollideCheck<Solid>(player.Position + Vector2.UnitX * 5f);
                                    if (flag8)
                                    {
                                        player.MoveHExact(5, null, null);
                                    }
                                }
                                bool flag9 = player.ClimbCheck(-1, 0);
                                bool flag10 = player.ClimbCheck(1, 0);
                                int moveX = data.Get<int>("moveX");
                                bool flag11 = Input.Grab.Check && ((moveX == 1 && flag10) || (moveX == -1 && flag9));
                                if (flag11)
                                {
                                    player.Facing = (Facings)moveX;
                                    bool noGrabbing = SaveData.Instance.Assists.NoGrabbing;
                                    if (!noGrabbing)
                                    {
                                        return 1;
                                    }
                                    player.ClimbTrigger(moveX);
                                    player.Speed.X = 0f;
                                }
                            }
                        }
                        return 0;
                    }
                }
            }
            else
            {
                // new property
                data.Set("customDreamBlock", dreamBlock);
                if (player.Scene.OnInterval(0.1f))
                {
                    CreateTrail(player);
                }
                if (player.SceneAs<Level>().OnInterval(0.04f))
                {
                    DisplacementRenderer.Burst burst = player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.3f, 0f, 40f, 1f, null, null);
                    burst.WorldClipCollider = dreamBlock.Collider;
                    burst.WorldClipPadding = 2;
                }
                if (dreamBlock.AllowRedirects && player.CanDash)
                {
                    bool sameDir = Input.GetAimVector(Facings.Right) == player.DashDir;
                    bool flag4 = !sameDir || dreamBlock.AllowRedirectsInSameDir;
                    if (flag4)
                    {
                        player.DashDir = Input.GetAimVector(Facings.Right);
                        player.Speed = player.DashDir * player.Speed.Length();
                        player.Dashes = Math.Max(0, player.Dashes - 1);
                        Audio.Play("event:/char/madeline/dreamblock_enter");
                        if (sameDir)
                        {
                            player.Speed *= dreamBlock.SameDirectionSpeedMultiplier;
                            player.DashDir *= (float)Math.Sign(dreamBlock.SameDirectionSpeedMultiplier);
                        }
                        Input.Dash.ConsumeBuffer();
                    }
                }
            }
            return FrostModule.CustomDreamDashState;
        }

        public static void DreamDashBegin()
        {
            Player player = FrostModule.StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            SoundSource dreamSfxLoop = data.Get<SoundSource>("dreamSfxLoop");
            bool flag = dreamSfxLoop == null;
            if (flag)
            {
                dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
                data.Set("dreamSfxLoop", dreamSfxLoop);
            }
            player.Speed = player.DashDir * 240f;
            player.TreatNaive = true;
            player.Depth = -12000;
            data.Set("dreamDashCanEndTimer", 0.1f);
            player.Stamina = 110f;
            data.Set("dreamJump", false);
            player.Play("event:/char/madeline/dreamblock_enter", null, 0f);
            player.Loop(dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
        }

        public static void DreamDashEnd()
        {
            Player player = FrostModule.StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            player.Depth = 0;
            if (!data.Get<bool>("dreamJump"))
            {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
            }
            bool flag2 = !player.Inventory.NoRefills;
            if (flag2)
            {
                player.RefillDash();
            }
            player.RefillStamina();
            player.TreatNaive = false;
            CustomDreamBlock dreamBlock = data.Get<CustomDreamBlock>("customDreamBlock");
            if (dreamBlock != null)
            {
                bool flag4 = player.DashDir.X != 0f;
                if (flag4)
                {
                    data.Set("jumpGraceTimer", 0.1f);
                    data.Set("dreamJump", true);
                }
                else
                {
                    data.Set("jumpGraceTimer", 0f);
                }
                dreamBlock.OnPlayerExit(player);
                data.Set<CustomDreamBlock>("customDreamBlock", null);
            }
            player.Stop(data.Get<SoundSource>("dreamSfxLoop"));
            player.Play("event:/char/madeline/dreamblock_exit", null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        // Copy-Pasted from the Player class
        private static bool DreamDashedIntoSolid(Player player)
        {
            bool flag = player.CollideCheck<Solid>();
            bool result;
            if (flag)
            {
                for (int i = 1; i <= 5; i++)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            for (int l = -1; l <= 1; l += 2)
                            {
                                Vector2 value = new Vector2((float)(i * j), (float)(k * l));
                                bool flag2 = !player.CollideCheck<Solid>(player.Position + value);
                                if (flag2)
                                {
                                    player.Position += value;
                                    return false;
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        private static void CreateTrail(Player player)
        {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
            TrailManager.Add(player, scale, player.GetCurrentTrailColor(), 1f);
        }

        public static bool DreamDashCheck(Player player, Vector2 dir)
        {
            DynData<Player> data = new DynData<Player>(player);
            bool flag = player.Inventory.DreamDash && player.DashAttacking && (dir.X == (float)Math.Sign(player.DashDir.X) || dir.Y == (float)Math.Sign(player.DashDir.Y));
            if (flag)
            {
                CustomDreamBlock dreamBlock = player.CollideFirst<CustomDreamBlock>(player.Position + dir);
                bool flag2 = dreamBlock != null;
                if (flag2)
                {
                    bool flag3 = player.CollideCheck<Solid, CustomDreamBlock>(player.Position + dir);
                    if (flag3)
                    {
                        Vector2 value = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
                        bool flag4 = dir.X != 0f;
                        bool flag5;
                        bool flag6;
                        if (flag4)
                        {
                            flag5 = (player.Speed.Y <= 0f);
                            flag6 = (player.Speed.Y >= 0f);
                        }
                        else
                        {
                            flag5 = (player.Speed.X <= 0f);
                            flag6 = (player.Speed.X >= 0f);
                        }
                        if (flag5)
                        {
                            for (int i = -1; i >= -4; i--)
                            {
                                Vector2 at = player.Position + dir + value * (float)i;
                                bool flag8 = !player.CollideCheck<Solid, CustomDreamBlock>(at);
                                if (flag8)
                                {
                                    player.Position += value * (float)i;
                                    data.Set<CustomDreamBlock>("customDreamBlock",dreamBlock);
                                    return true;
                                }
                            }
                        }
                        if (flag6)
                        {
                            for (int j = 1; j <= 4; j++)
                            {
                                Vector2 at2 = player.Position + dir + value * (float)j;
                                bool flag10 = !player.CollideCheck<Solid, CustomDreamBlock>(at2);
                                if (flag10)
                                {
                                    player.Position += value * (float)j;
                                    data.Set<CustomDreamBlock>("customDreamBlock", dreamBlock);
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    data.Set<CustomDreamBlock>("customDreamBlock", dreamBlock);
                    return true;
                }
            }
            return false;
        }

        #region Hooks
        // Hook initialization
        public static void Load()
        {
            // legacy
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.RefillDash += Player_RefillDash;
            
        }

        public static void Unload()
        {
            // legacy
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.RefillDash -= Player_RefillDash;
        }

        private static bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
        {
            if (self.StateMachine.State != FrostModule.CustomDreamDashState)
                return orig(self);
            return false;
        }

        

        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
        {
            if (self.StateMachine.State == Player.StDash || self.StateMachine.State == Player.StRedDash)
            {
                bool flag14 = DreamDashCheck(self, Vector2.UnitY * (float)Math.Sign(self.Speed.Y));
                if (flag14)
                {
                    self.StateMachine.State = FrostModule.CustomDreamDashState;
                    DynData<Player> ddata = new DynData<Player>(self);
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != FrostModule.CustomDreamDashState)
            {
                orig(self, data);
            }
        }

        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
        {
            if (self.StateMachine.State == Player.StDash || self.StateMachine.State == Player.StRedDash)
            {
                bool flag14 = DreamDashCheck(self, Vector2.UnitX * (float)Math.Sign(self.Speed.X));
                if (flag14)
                {
                    self.StateMachine.State = FrostModule.CustomDreamDashState;
                    DynData<Player> ddata = new DynData<Player>(self);
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != FrostModule.CustomDreamDashState)
            {
                orig(self, data);
            }
        }
        #endregion
    }
}
