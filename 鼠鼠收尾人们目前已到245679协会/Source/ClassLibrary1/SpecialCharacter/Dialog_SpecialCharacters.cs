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
        private Vector2 detailScroll;
        private SpecialCharacterDef selected;
        private Pawn displayPawn;
        private string detailText;
        private string reply;
        private int balance;

        // Quality Colors (Continued) 1.6 的 Default 品质配色，仅用于详情装备文字。
        private static readonly string[] QualityColors =
            { "FF0000", "9E6700", "FFFFFF", "00FF00", "73ABFF", "B183BD", "FFFF00" };

        public override Vector2 InitialSize => new Vector2(840f, 740f);

        public Dialog_SpecialCharacters(Pawn negotiator)
        {
            this.negotiator = negotiator;
            map = negotiator.Map;
            service = Find.World.GetComponent<WorldComponent_SpecialCharacters>();
            service.InitializeForContact();
            characters = DefDatabase<SpecialCharacterDef>.AllDefsListForReading.OrderBy(d => d.index).ToArray();
            balance = WorldComponent_SpecialCharacters.SilverBalance(map);
            logo = ContentFinder<Texture2D>.Get("SpecialCharacter/Hana_Association");
            reply = "TRK_SC_Greeting".Translate();
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            if (selected == null) DrawOverview(rect);
            else DrawDetails(rect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawOverview(Rect rect)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, 0f, rect.width, 28f), "TRK_SC_Title".Translate());
            GUI.DrawTexture(new Rect((rect.width - 152f) / 2f, 30f, 152f, 152f), logo, ScaleMode.ScaleToFit);
            Widgets.Label(new Rect(0f, 188f, rect.width, 25f), "TRK_SC_Contact".Translate());
            Widgets.Label(new Rect(16f, 218f, rect.width - 32f, 48f), reply);
            DrawBalance(new Rect(0f, 270f, rect.width, 28f));
            Text.Anchor = TextAnchor.UpperLeft;

            Rect viewport = new Rect(0f, 306f, rect.width, rect.height - 366f);
            const int columns = 3;
            float width = (viewport.width - 20f) / columns;
            int rows = (characters.Length + columns - 1) / columns;
            Rect view = new Rect(0f, 0f, viewport.width - 20f, Math.Max(viewport.height, rows * 264f));
            SpecialCharacterDef details = null;
            Widgets.BeginScrollView(viewport, ref scroll, view);
            for (int i = 0; i < characters.Length; i++)
                if (DrawCharacter(characters[i], new Rect((i % columns) * width, (i / columns) * 264f, width - 12f, 252f)))
                    details = characters[i];
            Widgets.EndScrollView();
            if (details != null) OpenDetails(details);
            if (Widgets.ButtonText(new Rect((rect.width - 160f) / 2f, rect.height - 46f, 160f, 38f), "Close".Translate()))
                Close();
        }

        private bool DrawCharacter(SpecialCharacterDef def, Rect card)
        {
            Rect image = new Rect(card.x + (card.width - 112f) / 2f, card.y, 112f, 112f);
            GUI.DrawTexture(image, def.Icon, ScaleMode.ScaleToFit);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(card.x, card.y + 120f, card.width, 26f), def.LabelCap);
            Widgets.Label(new Rect(card.x, card.y + 149f, card.width, 26f), def.association);
            Text.Anchor = TextAnchor.UpperLeft;
            bool details = Widgets.ButtonText(new Rect(card.x + 12f, card.y + 184f, card.width - 24f, 38f),
                "TRK_SC_Details".Translate());
            SpecialCharacterOrder order = service.GetOrder(def);
            if (order != null)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(card.x, card.y + 224f, card.width, 26f), OrderLabel(order));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            return details;
        }

        private void DrawDetails(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, 0f, rect.width, 36f), selected.LabelCap);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 40f, rect.width, 26f), selected.association);
            Widgets.Label(new Rect(16f, 74f, rect.width - 32f, 48f), reply);
            DrawBalance(new Rect(0f, 126f, rect.width, 28f));
            Text.Anchor = TextAnchor.UpperLeft;

            float leftWidth = rect.width * 0.4f;
            Rect portrait = new Rect(0f, 174f, leftWidth, rect.height - 236f);
            Rect info = new Rect(leftWidth + 18f, portrait.y, rect.width - leftWidth - 18f, portrait.height);
            Widgets.DrawMenuSection(portrait);
            Widgets.DrawMenuSection(info);
            Rect projection = portrait.ContractedBy(12f);
            if (displayPawn != null && !displayPawn.Destroyed)
                GUI.DrawTexture(projection, PortraitsCache.Get(displayPawn, projection.size, Rot4.South,
                    cameraZoom: 0.9f, healthStateOverride: PawnHealthState.Mobile), ScaleMode.ScaleToFit);
            else
                Widgets.Label(projection, ModsConfig.BiotechActive
                    ? "TRK_SC_PortraitUnavailable".Translate() : "TRK_SC_BiotechRequired".Translate());

            Rect viewport = info.ContractedBy(12f);
            Rect view = new Rect(0f, 0f, viewport.width - 18f,
                Math.Max(viewport.height, Text.CalcHeight(detailText, viewport.width - 18f)));
            Widgets.BeginScrollView(viewport, ref detailScroll, view);
            Widgets.Label(view, detailText);
            Widgets.EndScrollView();

            Rect back = new Rect(0f, rect.height - 46f, 160f, 38f);
            if (Widgets.ButtonText(back, "TRK_SC_Back".Translate()))
            {
                ClearDetails();
                return;
            }
            DrawPurchase(selected, new Rect(rect.width - 220f, back.y, 220f, back.height));
        }

        private void DrawBalance(Rect rect)
        {
            string label = "TRK_SC_Balance".Translate(balance).ToString();
            float width = Text.CalcSize(label).x + 36f;
            float x = rect.x + (rect.width - width) / 2f;
            Widgets.ThingIcon(new Rect(x, rect.y, 26f, 26f), ThingDefOf.Silver);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x + 36f, rect.y, width - 36f, rect.height), label);
        }

        private void DrawPurchase(SpecialCharacterDef def, Rect button)
        {
            SpecialCharacterOrder order = service.GetOrder(def);
            string unavailable = service.UnavailableReason(def, map);
            bool refund = order != null && order.lifecycle.stage == DispatchStage.Failed && order.payment.Count > 0;
            string label = refund ? "TRK_SC_Refund".Translate().ToString() :
                order != null ? OrderLabel(order) : "TRK_SC_Buy".Translate(def.price).ToString();
            bool enabled = refund || (unavailable == null && balance >= def.price);
            bool clicked = Widgets.ButtonText(button, label, active: enabled);
            if (order == null)
                Widgets.ThingIcon(new Rect(button.xMax - 36f, button.y + 6f, 26f, 26f), ThingDefOf.Silver,
                    alpha: enabled ? 1f : 0.5f);
            if (clicked)
            {
                if (refund)
                    reply = service.Refund(order, map, negotiator.Position)
                        ? "TRK_SC_Refunded".Translate() : "TRK_SC_RefundFailed".Translate();
                else service.Purchase(def, negotiator, out reply);
                balance = WorldComponent_SpecialCharacters.SilverBalance(map);
            }
            if (!enabled)
                TooltipHandler.TipRegion(button, unavailable ?? "TRK_SC_InsufficientSilver".Translate().ToString());
        }

        private static string OrderLabel(SpecialCharacterOrder order)
        {
            return (order.lifecycle.stage == DispatchStage.Waiting || order.lifecycle.stage == DispatchStage.InFlight)
                ? "TRK_SC_Dispatching".Translate().ToString() :
                (order.lifecycle.stage == DispatchStage.Failed ? "TRK_SC_Failed".Translate().ToString() :
                    "TRK_SC_Purchased".Translate().ToString());
        }

        private void OpenDetails(SpecialCharacterDef def)
        {
            ClearDetails();
            Pawn pawn = ModsConfig.BiotechActive ? service.GetCharacter(def, map) : null;
            detailText = CharacterInfo(def);
            displayPawn = pawn;
            detailScroll = Vector2.zero;
            selected = def;
        }

        private void ClearDetails()
        {
            displayPawn = null;
            selected = null;
            detailText = null;
        }

        public override void PostClose()
        {
            ClearDetails();
            base.PostClose();
        }

        private static string CharacterInfo(SpecialCharacterDef def)
        {
            StringBuilder text = new StringBuilder(def.description);
            text.AppendLine().AppendLine();
            text.AppendLine("TRK_SC_FixedProfile".Translate(def.gender.GetLabel(), def.biologicalAge,
                def.pawnKind.race.LabelCap.ToString()).ToString());
            text.AppendLine("TRK_SC_Traits".Translate(string.Join("、", def.pawnKind.forcedTraits
                .Select(t => t.def.DataAtDegree(t.degree.Value).GetLabelCapFor(def.gender)))).ToString());
            text.AppendLine();
            foreach (SpecialCharacterSkill skill in def.skills)
                text.AppendLine(skill.skill.LabelCap.ToString() + ": " + skill.level +
                    (skill.passion == Passion.Major ? " ★★" : skill.passion == Passion.Minor ? " ★" : ""));
            text.AppendLine().Append("TRK_SC_Equipment".Translate().ToString());
            text.Append(string.Join("、", new[] { def.weapon }.Concat(def.apparel).Select(EquipmentLabel)));
            text.AppendLine().Append("TRK_SC_Inventory".Translate().ToString());
            text.Append(string.Join("、", def.inventory.Select(item => item.thing.LabelCap.ToString() + "×" + item.count)));
            text.AppendLine().AppendLine().Append("TRK_SC_OneTimeRule".Translate().ToString());
            text.AppendLine().Append("TRK_SC_Report".Translate(def.detailedReport).ToString());
            return text.ToString();
        }

        private static string EquipmentLabel(SpecialCharacterItem item)
        {
            return "<color=#" + QualityColors[(int)item.quality] + ">" + item.thing.LabelCap.ToString() +
                "（" + item.quality.GetLabel() + "）</color>";
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
