using System;
using RimWorld;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    // 专用空投载体只通知其所属订单，不监听角色之后的地图出入事件。
    public sealed class SpecialCharacterPod : ActiveTransporter
    {
        public SpecialCharacterDef character;
        private bool failed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref character, "character");
            Scribe_Values.Look(ref failed, "failed");
        }

        protected override void Tick()
        {
            if (failed || !Spawned) return;
            try
            {
                base.Tick();
            }
            catch (Exception ex)
            {
                failed = true;
                Find.World.GetComponent<WorldComponent_SpecialCharacters>().PodFailed(this, ex.ToString());
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            WorldComponent_SpecialCharacters service = Find.World?.GetComponent<WorldComponent_SpecialCharacters>();
            base.Destroy(mode);
            service?.PodDestroyed(this);
        }
    }

}
