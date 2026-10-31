using RimWorld;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    // 专用空投载体只通知其所属订单，不监听角色之后的地图出入事件。
    public sealed class SpecialCharacterPod : ActiveTransporter
    {
        public SpecialCharacterDef character;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref character, "character");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            WorldComponent_SpecialCharacters service = Find.World?.GetComponent<WorldComponent_SpecialCharacters>();
            base.Destroy(mode);
            service?.PodDestroyed(this);
        }
    }

}
