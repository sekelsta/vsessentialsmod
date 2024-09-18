﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class EntityBehaviorRepulseAgents : EntityBehavior
    {
        Vec3d pushVector = new Vec3d();
        EntityPartitioning partitionUtil;
        bool movable = true;
        bool ignorePlayers = false;
        EntityAgent selfEagent;

        public EntityBehaviorRepulseAgents(Entity entity) : base(entity)
        {
            entity.hasRepulseBehavior = true;

            selfEagent = entity as EntityAgent;
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            movable = attributes["movable"].AsBool(true);
            partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
            ignorePlayers = entity is EntityPlayer && entity.World.Config.GetAsBool("player2PlayerCollisions", true);
        }

        double ownPosRepulseX, ownPosRepulseY, ownPosRepulseZ;
        float mySize;

        public override void OnGameTick(float deltaTime)
        {
            if (entity.State == EnumEntityState.Inactive || !entity.IsInteractable || !movable || (entity is EntityAgent eagent && eagent.MountedOn != null)) return;
            if (entity.World.ElapsedMilliseconds < 2000) return;
            double touchdist = entity.SelectionBox.XSize / 2;

            pushVector.Set(0, 0, 0);

            ownPosRepulseX = entity.ownPosRepulse.X;
            ownPosRepulseY = entity.ownPosRepulse.Y + entity.Pos.DimensionYAdjustment;
            ownPosRepulseZ = entity.ownPosRepulse.Z;
            mySize = entity.SelectionBox.Length * entity.SelectionBox.Height;

            if (selfEagent != null && selfEagent.Controls.Sneak) mySize *= 2;


            partitionUtil.WalkEntityPartitions(entity.ownPosRepulse, touchdist + partitionUtil.LargestTouchDistance + 0.1, WalkEntity);

            pushVector.X = GameMath.Clamp(pushVector.X, -3, 3);
            pushVector.Y = GameMath.Clamp(pushVector.Y, -3, 0.5);
            pushVector.Z = GameMath.Clamp(pushVector.Z, -3, 3);

            entity.SidedPos.Motion.Add(pushVector.X / 30, pushVector.Y / 30, pushVector.Z / 30);
        }


        private bool WalkEntity(Entity e)
        {
            if (!e.hasRepulseBehavior || !e.IsInteractable || e == entity || (ignorePlayers && e is EntityPlayer)) return true;
            if (e is EntityAgent eagent && eagent.MountedOn?.Entity == entity) return true;

            double dx = ownPosRepulseX - e.ownPosRepulse.X;
            double dy = ownPosRepulseY - e.ownPosRepulse.Y;
            double dz = ownPosRepulseZ - e.ownPosRepulse.Z;

            double distSq = dx * dx + dy * dy + dz * dz;
            double minDistSq = entity.touchDistanceSq + e.touchDistanceSq;

            if (distSq >= minDistSq) return true;
            
            double pushForce = (1 - distSq / minDistSq) / Math.Max(0.001f, GameMath.Sqrt(distSq));
            double px = dx * pushForce;
            double py = dy * pushForce;
            double pz = dz * pushForce;

            float hisSize = e.SelectionBox.Length * e.SelectionBox.Height;

            float pushDiff = GameMath.Clamp(hisSize / mySize, 0, 1);

            if (entity.OnGround) pushDiff *= 3;
            
            pushVector.Add(px * pushDiff, py * pushDiff * 0.75, pz * pushDiff);

            return true;
        }


        public override string PropertyName()
        {
            return "repulseagents";
        }
    }
}
