using System.Collections.Generic;
using Verse;

namespace XIYUNTE
{
    // 单个蓄力阶的数据:该阶使用的弹丸、瞄准时长与武器精度加成。
    public class DeathBowStage
    {
        public ThingDef projectile;

        public float warmupTime;

        public float accuracyOffset;

        // 可选:该阶按钮图标路径(相对 Textures,不含扩展名);留空时使用武器图标。
        public string iconPath;

        // 可选:该阶蓄力瞄准时是否显示瞄准特效(烟雾),默认关闭;当前只有四阶开启。
        public bool aimVfx;

        // 该阶介绍文字,必需。用于按钮悬停说明、阶段选择菜单提示与信息卡当前阶段说明。
        public string description;

        // 切到该阶时播放的音效,必需。
        public SoundDef switchSound;
    }

    // 死之弓组件属性:蓄力阶列表、补充次数所用的弹药与次数上限。
    public class Properties_DeathBow : CompProperties
    {
        public List<DeathBowStage> stages = new List<DeathBowStage>();

        public ThingDef ammoDef;

        public int maxCharges = 4;

        // 蓄力阶切换耗时(tick)。0 = 立即切换;大于 0 时切换期间无法射击。
        public int stageSwitchTicks;

        // 近战/远程模式切换耗时(tick),与阶段切换相互独立。0 = 立即切换。
        public int modeSwitchTicks;

        // 近战/远程模式切换音效,必需。
        public SoundDef modeSwitchSound;

        // 装填死之箭的音效,必需。
        public SoundDef loadSound;

        // 可选:模式按钮与装填按钮的图标路径;留空时分别使用武器图标与弹药图标。
        public string modeIconPath;

        public string loadIconPath;

        public Properties_DeathBow()
        {
            compClass = typeof(CompEquippable_DeathBow);
        }

        // 配置缺失在 Def 加载阶段直接报错,不做静默回退。
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (stages.NullOrEmpty())
            {
                yield return "DeathBow has no stage configured.";
                yield break;
            }
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].projectile == null)
                {
                    yield return "DeathBow stage " + i + " has no projectile.";
                }
                if (stages[i].warmupTime <= 0f)
                {
                    yield return "DeathBow stage " + i + " has non-positive warmupTime.";
                }
                if (stages[i].description.NullOrEmpty())
                {
                    yield return "DeathBow stage " + i + " has no description.";
                }
                if (stages[i].iconPath.NullOrEmpty())
                {
                    yield return "DeathBow stage " + i + " has no iconPath.";
                }
                if (stages[i].switchSound == null)
                {
                    yield return "DeathBow stage " + i + " has no switchSound.";
                }
            }
            if (modeSwitchSound == null)
            {
                yield return "DeathBow has no modeSwitchSound configured.";
            }
            if (loadSound == null)
            {
                yield return "DeathBow has no loadSound configured.";
            }
            if (ammoDef == null)
            {
                yield return "DeathBow has no ammoDef configured.";
            }
            if (maxCharges <= 0)
            {
                yield return "DeathBow maxCharges must be positive.";
            }
        }
    }
}
