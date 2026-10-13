using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace XIYUNTE.SpecialCharacter
{
    public sealed class Dialog_SpecialCharacters : Window
    {
        private readonly Pawn negotiator;
        private readonly Map map;
        private readonly WorldComponent_SpecialCharacters service;
        private readonly SpecialCharacterDef[] characters;
        private readonly Texture2D logo;
        private Vector2 scroll;
        private string reply;
        private int balance;

        public override Vector2 InitialSize => new Vector2(840f, 740f);

        public Dialog_SpecialCharacters(Pawn negotiator)
        {
            this.negotiator = negotiator;
            map = negotiator.Map;
            service = Find.World.GetComponent<WorldComponent_SpecialCharacters>();
            characters = DefDatabase<SpecialCharacterDef>.AllDefsListForReading.OrderBy(d => d.index).ToArray();
            balance = WorldComponent_SpecialCharacters.SilverBalance(map);
            logo = ContentFinder<Texture2D>.Get("SpecialCharacter/Hana_Association");
            reply = "TRK_SC_Greeting".Translate();
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, 0f, rect.width, 28f), "TRK_SC_Title".Translate());
            GUI.DrawTexture(new Rect((rect.width - 152f) / 2f, 30f, 152f, 152f), logo, ScaleMode.ScaleToFit);
            Widgets.Label(new Rect(0f, 188f, rect.width, 25f), "TRK_SC_Contact".Translate());
            Widgets.Label(new Rect(16f, 218f, rect.width - 32f, 48f), reply);
            Widgets.Label(new Rect(0f, 270f, rect.width, 26f), "TRK_SC_Balance".Translate(balance));
            Text.Anchor = TextAnchor.UpperLeft;

            Rect viewport = new Rect(0f, 306f, rect.width, rect.height - 366f);
            const int columns = 3;
            float width = (viewport.width - 20f) / columns;
            int rows = (characters.Length + columns - 1) / columns;
            Rect view = new Rect(0f, 0f, viewport.width - 20f, Math.Max(viewport.height, rows * 264f));
            Widgets.BeginScrollView(viewport, ref scroll, view);
            for (int i = 0; i < characters.Length; i++)
                DrawCharacter(characters[i], new Rect((i % columns) * width, (i / columns) * 264f, width - 12f, 252f));
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawCharacter(SpecialCharacterDef def, Rect card)
        {
            Rect image = new Rect(card.x + (card.width - 112f) / 2f, card.y, 112f, 112f);
            GUI.DrawTexture(image, def.Icon, ScaleMode.ScaleToFit);
            TooltipHandler.TipRegion(card, CharacterInfo(def));
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(card.x, card.y + 120f, card.width, 26f), def.LabelCap);
            Widgets.Label(new Rect(card.x, card.y + 149f, card.width, 26f), def.association);
            Text.Anchor = TextAnchor.UpperLeft;
            SpecialCharacterOrder order = service.GetOrder(def);
            string label;
            string unavailable = service.UnavailableReason(def, map);
            bool refund = order != null && order.lifecycle.stage == DispatchStage.Failed && order.payment.Count > 0;
            if (refund) label = "TRK_SC_Refund".Translate();
            else if (order != null)
                label = (order.lifecycle.stage == DispatchStage.Waiting || order.lifecycle.stage == DispatchStage.InFlight)
                    ? "TRK_SC_Dispatching".Translate() :
                    (order.lifecycle.stage == DispatchStage.Failed ? "TRK_SC_Failed".Translate() : "TRK_SC_Purchased".Translate());
            else label = "TRK_SC_Buy".Translate(def.price);
            bool enabled = refund || (unavailable == null && balance >= def.price);
            if (Widgets.ButtonText(new Rect(card.x + 12f, card.y + 184f, card.width - 24f, 38f), label, active: enabled))
            {
                if (refund)
                {
                    reply = service.Refund(order, map, negotiator.Position)
                        ? "TRK_SC_Refunded".Translate() : "TRK_SC_RefundFailed".Translate();
                }
                else service.Purchase(def, negotiator, out reply);
                balance = WorldComponent_SpecialCharacters.SilverBalance(map);
            }
            if (order == null && unavailable != null)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(card.x, card.y + 224f, card.width, 26f), unavailable);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else if (order == null && balance < def.price)
                TooltipHandler.TipRegion(new Rect(card.x + 12f, card.y + 184f, card.width - 24f, 38f),
                    "TRK_SC_InsufficientSilver".Translate());
        }

        private static string CharacterInfo(SpecialCharacterDef def)
        {
            StringBuilder text = new StringBuilder(def.description);
            text.AppendLine().AppendLine();
            text.AppendLine("TRK_SC_FixedProfile".Translate(def.gender.GetLabel(), def.biologicalAge));
            foreach (SpecialCharacterSkill skill in def.skills)
                text.AppendLine(skill.skill.LabelCap + ": " + skill.level +
                    (skill.passion == Passion.Major ? " ★★" : skill.passion == Passion.Minor ? " ★" : ""));
            text.AppendLine().Append(def.weapon.thing.LabelCap);
            foreach (SpecialCharacterItem apparel in def.apparel) text.Append("、").Append(apparel.thing.LabelCap);
            text.AppendLine().Append("TRK_SC_OneTimeRule".Translate());
            return text.ToString();
        }
    }

    // 仅向鼠族王国原有通讯对话增加独立派遣入口。
    [HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
    public static class Patch_RatkinKingdomSpecialCharacters
    {
        public static void Postfix(Pawn negotiator, Faction faction, DiaNode __result)
        {
            if (faction.def.defName != "Rakinia" || negotiator.Map == null || !negotiator.Map.IsPlayerHome) return;
            __result.options.Insert(0, new DiaOption("TRK_SC_OpenDispatch".Translate())
            {
                resolveTree = true,
                action = () => Find.WindowStack.Add(new Dialog_SpecialCharacters(negotiator))
            });
        }
    }
}
