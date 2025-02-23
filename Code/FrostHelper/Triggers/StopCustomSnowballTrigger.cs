﻿using Celeste.Mod.Entities;

namespace FrostHelper;

[CustomEntity("FrostHelper/StopCustomSnowballTrigger")]
public class StopCustomSnowballTrigger : Trigger {
    public StopCustomSnowballTrigger(EntityData data, Vector2 offset) : base(data, offset) {
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        foreach (CustomSnowball snowball in Scene.Entities.FindAll<CustomSnowball>()) {
            snowball.StartLeaving();
        }
    }
}
