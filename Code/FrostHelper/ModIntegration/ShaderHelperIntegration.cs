﻿namespace FrostHelper.ModIntegration;

public static class ShaderHelperIntegration {
    public class MissingShaderException : Exception {
        private string id;
        public MissingShaderException(string id) : base() {
            this.id = id;
        }

        public override string Message => $"Shader not found: {id}";
    }

    public class NotLoadedException : Exception {
        public NotLoadedException() : base() {
        }

        public override string Message => "Shader Helper is not installed, but Frost Helper tried getting an effect!\nAdd ShaderHelper as a dependency in your everest.yaml!";
    }

    [OnLoadContent]
    public static void Load() {
        EverestModuleMetadata shaderHelperMeta = new EverestModuleMetadata { Name = "ShaderHelper", VersionString = "0.0.3" };
        if (IntegrationUtils.TryGetModule(shaderHelperMeta, out EverestModule shaderHelperModule)) {
            module = shaderHelperModule;
            module_FX = shaderHelperModule.GetType().GetField("FX");
            Loaded = true;
        }
    }

    private static bool _loaded;
    public static bool Loaded {
        set => _loaded = value;
        get {
            if (!_loaded)
                Load();
            return _loaded;
        }
    }

    private static FieldInfo module_FX;
    private static EverestModule module;

    /// <summary>
    /// Used for caching if Shader Helper is not installed
    /// </summary>
    private static Dictionary<string, Effect> FallbackEffectDict = new();

    private static Dictionary<string, Effect> GetShaderHelperEffects() => Loaded ? (module_FX.GetValue(module) as Dictionary<string, Effect>)! : FallbackEffectDict;

    public static Effect GetEffect(string id) {
        if (GetShaderHelperEffects().TryGetValue(id, out var eff)) {
            return eff;
        }

        // try to load the effect if Shader Helper isn't installed or it didn't find it
        if (Everest.Content.TryGet("Effects/" + id, out var effectAsset, true)) {
            try {
                Effect effect = new Effect(Engine.Graphics.GraphicsDevice, effectAsset.Data);
                GetShaderHelperEffects().Add(id, effect);
                return effect;
            } catch (Exception ex) {
                Logger.Log(LogLevel.Error, "FrostHelper", "Failed to load the shader " + id);
                Logger.Log(LogLevel.Error, "FrostHelper", "Exception: \n" + ex.ToString());
            }
        }

        throw new MissingShaderException(id);
        //throw new NotLoadedException();
    }

    public static Effect BeginGameplayRenderWithEffect(string id, bool endBatch) {
        if (!Loaded)
            throw new NotLoadedException();

        Effect effect = GetEffect(id);

        ApplyStandardParameters(effect);

        if (endBatch) {
            Draw.SpriteBatch.End();
        }

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect, FrostModule.GetCurrentLevel().Camera.Matrix);

        return effect;
    }

    public static void DrawWithEffect(string id, Action drawFunc, bool endBatch) {
        /*
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);
        BeginGameplayRenderWithEffect(id, endBatch);

        drawFunc();

        Draw.SpriteBatch.End();
        if (endBatch)
        {
            GameplayRenderer.Begin();
        }*/
        Camera c = FrostModule.GetCurrentLevel().Camera;
        VirtualRenderTarget tempA = GameplayBuffers.TempA;
        GameplayRenderer.End();
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);
        int s = GameplayBuffers.Gameplay.Width / 320;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, c.Matrix * Matrix.CreateScale(s));

        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        drawFunc();

        GameplayRenderer.End();

        Effect eff = GetEffect(id);
        eff.Parameters["Time"].SetValue(Engine.Scene.TimeActive);
        eff.Parameters["camPos"].SetValue(c.Position);

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, Matrix.Identity);


        Draw.SpriteBatch.Draw(tempA, Vector2.Zero, Color.White);

        GameplayRenderer.End();
        GameplayRenderer.Begin();
    }

    public static void ApplyStandardParameters(Effect effect) {
        var level = FrostModule.GetCurrentLevel() ?? throw new Exception("Not in a level when applying shader parameters! How did you...");
        var viewport = Engine.Graphics.GraphicsDevice.Viewport;

        effect.Parameters["DeltaTime"]?.SetValue(Engine.DeltaTime);
        effect.Parameters["Time"]?.SetValue(Engine.Scene.TimeActive);
        effect.Parameters["Dimensions"]?.SetValue(new Vector2(viewport.Width, viewport.Height));
        effect.Parameters["CamPos"]?.SetValue(level.Camera.Position);
        effect.Parameters["ColdCoreMode"]?.SetValue(level.CoreMode == Session.CoreModes.Cold);
    }

    public static void ApplyParametersFrom(this Effect shader, Dictionary<string, string> parameters) {
        foreach (var item in parameters) {
            var prop = shader.Parameters[item.Key];
            if (prop is null) {
                throw new Exception($"Shader doesn't have a {item.Key} property!");
            }


            switch (prop.ParameterType) {
                case EffectParameterType.Bool:
                    prop.SetValue(Convert.ToBoolean(item.Value));
                    break;
                case EffectParameterType.Single:
                    prop.SetValue(item.Value.ToSingle());
                    break;
                default:
                    throw new Exception($"Entity Batcher doesn't know how to set a parameter of type {prop.ParameterType} for property {item.Key}");
            }
        }
    }

    public static void ApplyParametersFrom(this Effect shader, Dictionary<string, object> parameters, bool throwOnMissingProp = true) {
        foreach (var item in parameters) {
            var prop = shader.Parameters[item.Key];
            if (prop is null) {
                if (throwOnMissingProp)
                    throw new Exception($"Shader doesn't have a {item.Key} property!");
                else
                    continue;
            }


            switch (prop.ParameterType) {
                case EffectParameterType.Bool:
                    prop.SetValue(Convert.ToBoolean(item.Value));
                    break;
                case EffectParameterType.Single:
                    switch (item.Value) {
                        case string str:
                            prop.SetValue(str.ToSingle());
                            break;
                        case float f:
                            prop.SetValue(f);
                            break;
                    }
                    
                    break;
                default:
                    throw new Exception($"Entity Batcher doesn't know how to set a parameter of type {prop.ParameterType} for property {item.Key}");
            }
        }
    }
}
