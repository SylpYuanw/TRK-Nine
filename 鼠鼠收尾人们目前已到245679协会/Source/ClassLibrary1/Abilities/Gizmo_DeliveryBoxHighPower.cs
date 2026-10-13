using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace XIYUNTE
{
    // 高功率模式状态 Gizmo：仿原版护盾腰带/Flugel Gizmo_ShieldStatus。
    // 绘制「剩余时间(到严重度100%死亡)」与「护盾真实伤害条」两条，激活期间显示在角色栏。
    // 由 Hediff_DeliveryBoxHighPower.GetGizmos() 产出,即「Hediff 自绘的状态条」。
    public class Gizmo_DeliveryBoxHighPower : Gizmo
    {
        public Hediff_DeliveryBoxHighPower hediff;

        private static readonly Texture2D FullTimeBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(1.0f, 0.78f, 0.3f));   // 剩余时间(黄)
        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.0f, 0.5f, 0.25f)); // 护盾(深绿)
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_DeliveryBoxHighPower()
        {
            Order = -90f;
        }

        public Gizmo_DeliveryBoxHighPower(Hediff_DeliveryBoxHighPower hediff)
        {
            this.hediff = hediff;
            Order = -90f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 190f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            // 关闭词换行：避免「剩余时间：X」在宽度不足时折成两行,被色条高度裁掉后半行。
            bool wordWrap = Text.WordWrap;
            Text.WordWrap = false;

            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 84f);
            Widgets.DrawWindowBackground(rect);
            Rect inner = rect.ContractedBy(6f);

            // ---- 标题(本地化) ----
            Rect titleRect = inner;
            titleRect.height = 24f;
            GUI.color = new Color(1.0f, 0.85f, 0.5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(titleRect, "RK_DeliveryBox_HighPower_StatusTitle".Translate().ToString());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // ---- 剩余时间条(本地化,死亡前 3 小时/三阶段转警示红) ----
            // 仿 Shin 写「剩余时间」：用 ElapsedTicks 反推剩余 tick(1 游戏小时 = 2500 tick),换算成小时。
            HediffComp_DeliveryBoxHighPower mainComp = hediff?.TryGetComp<HediffComp_DeliveryBoxHighPower>();
            int remainTicks = Mathf.Max(0, (mainComp?.Props.totalTicks ?? 30000) - (mainComp?.ElapsedTicks ?? 0));
            float remainRatio = Mathf.Clamp01((float)remainTicks / Mathf.Max(1, mainComp?.Props.totalTicks ?? 30000));

            // 颜色条框
            Rect timeBar = inner;
            timeBar.yMin = inner.y + 24f;
            timeBar.height = 18f;
            Widgets.FillableBar(timeBar, remainRatio, FullTimeBarTex, EmptyBarTex, false);

            // 文字框 = 颜色条框(对齐且不小于文字),Tiny 居中,一行显示。
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            float remainHours = remainTicks / 2500f;
            // 死亡前 3 小时(进入三阶段/开始化学灼烧)时,剩余时间文字改用警示红 #FF1A1A；否则白色。
            GUI.color = remainTicks <= 7500
                ? new Color(1.0f, 0.10f, 0.10f)
                : Color.white;
            Rect timeLabelRect = timeBar;
            Widgets.Label(timeLabelRect, "RK_DeliveryBox_HighPower_RemainingTime".Translate(remainHours.ToString("0.0")).ToString());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // ---- 护盾真实伤害条(深绿、本地化、文字框与护盾框对齐) ----
            Rect shieldBar = inner;
            shieldBar.yMin = inner.y + 48f;
            shieldBar.height = 18f;
            HediffComp_DeliveryBoxShieldEnergy shieldComp = GetShieldComp();
            if (shieldComp != null)
            {
                float fill = Mathf.Clamp01(shieldComp.DamagePool / Mathf.Max(1f, shieldComp.Props.maxDamage));
                Widgets.FillableBar(shieldBar, fill, FullShieldBarTex, EmptyBarTex, false);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                // 文字框 = 护盾框(对齐且不小于文字),一行显示。
                Rect shieldLabelRect = shieldBar;
                Widgets.Label(shieldLabelRect, "RK_DeliveryBox_HighPower_ShieldValue".Translate(shieldComp.DamagePool.ToString("F0"), shieldComp.Props.maxDamage.ToString("F0")).ToString());
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                Rect shieldLabelRect = shieldBar;
                Widgets.Label(shieldLabelRect, "RK_DeliveryBox_HighPower_ShieldInactive".Translate().ToString());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // 还原文本设置。
            Text.WordWrap = wordWrap;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            return new GizmoResult(GizmoState.Clear);
        }

        private HediffComp_DeliveryBoxShieldEnergy GetShieldComp()
        {
            Pawn p = hediff?.pawn;
            if (p == null || p.health == null)
                return null;
            HediffDef shieldDef = DefDatabase<HediffDef>.GetNamedSilentFail("RK_DeliveryBox_HighPower_Shield");
            if (shieldDef == null)
                return null;
            Hediff shield = p.health.hediffSet.GetFirstHediffOfDef(shieldDef);
            return shield?.TryGetComp<HediffComp_DeliveryBoxShieldEnergy>();
        }
    }
}
