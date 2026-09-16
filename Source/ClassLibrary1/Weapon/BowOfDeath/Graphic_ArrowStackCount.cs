using UnityEngine;
using Verse;

namespace XIYUNTE
{
    // 死之箭堆叠贴图:按实际堆叠数量取图,1 支取 a、2 支取 b、3 支及以上取 c。
    // 原版 Graphic_StackCount 在 3 张贴图时的语义是"1 张/中间量/满堆",与本物品的单/双/多不符。
    // SubGraphicForStackCount 未标记 virtual,故从 virtual 入口 SubGraphicFor(Thing) 覆盖取图规则。
    public class Graphic_ArrowStackCount : Graphic_StackCount
    {
        // 取图规则:堆叠 1 取第一张、2 取第二张、3 及以上取第三张;无物品上下文或贴图不足 3 张时回退原版规则。
        public override Graphic SubGraphicFor(Thing thing)
        {
            if (thing != null && subGraphics.Length >= 3)
            {
                int count = thing.stackCount;
                if (count <= 1) return subGraphics[0];
                if (count == 2) return subGraphics[1];
                return subGraphics[2];
            }
            return base.SubGraphicFor(thing);
        }

        // 颜色化版本必须返回同一类型,否则染色后会退回原版 Graphic_StackCount 的取图规则。
        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_ArrowStackCount>(path, newShader, drawSize, newColor, newColorTwo, data);
        }
    }
}
