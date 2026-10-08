using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using XIYUNTE;

internal static class SimulationBootstrap
{
    public static int Main(string[] args)
    {
        string root = args[0];
        string[] folders = { Path.Combine(root, "Assemblies"), Path.Combine(root, "Source/ClassLibrary1/bin/Debug"), Path.Combine(root, "../本机源码/rimworlddll") };
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            foreach (string folder in folders)
            {
                string file = Path.Combine(folder, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try { Simulation.Run(root); return 0; }
        catch (Exception e)
        {
            while (e != null) { Console.Error.WriteLine(e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); e = e.InnerException; }
            return 1;
        }
    }
}

internal class SimulationPawn : Pawn
{
    public override Vector3 DrawPos { get { return Position.ToVector3Shifted(); } }
}

internal static class Simulation
{
    private delegate ThoughtState ReadState(Pawn pawn);
    private static HediffDef auraDef;
    private static HediffDef setDef;
    private static ThoughtDef thoughtDef;
    private static ReadState readState;
    private static readonly Func<long> allocatedBytes = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
    private static int checks;
    private static int cacheNotices;
    private static readonly List<FleckCreationData> flecks = new List<FleckCreationData>();
    private static readonly List<string> lines = new List<string>();

    private static T Raw<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }
    private static void Field(object instance, Type type, string field, object value) { type.GetField(field, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value); }
    private static void Check(bool success, string message) { checks++; if (!success) throw new Exception(message); }
    private static void Say(string message) { lines.Add(message); Console.WriteLine(message); }
    private static XmlDocument Read(string root, string path) { XmlDocument xml = new XmlDocument(); xml.Load(Path.Combine(root, path)); return xml; }
    private static float Number(string value) { return float.Parse(value, CultureInfo.InvariantCulture); }

    private static Pawn PawnFixture(int unrelated, bool full)
    {
        Pawn p = Raw<Pawn>();
        p.def = Raw<ThingDef>(); p.def.category = ThingCategory.Pawn; p.def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        p.kindDef = new PawnKindDef();
        p.health = Raw<Pawn_HealthTracker>(); Field(p.health, typeof(Pawn_HealthTracker), "pawn", p);
        p.health.hediffSet = Raw<HediffSet>(); Field(p.health.hediffSet, typeof(HediffSet), "pawn", p);
        p.health.hediffSet.hediffs = new List<Hediff>();
        p.needs = Raw<Pawn_NeedsTracker>(); Field(p.needs, typeof(Pawn_NeedsTracker), "needs", new List<Need>());
        for (int i = 0; i < unrelated; i++) p.health.hediffSet.hediffs.Add(new Hediff { def = new HediffDef(), pawn = p });
        if (full)
        {
            Hediff_LiuSet set = new Hediff_LiuSet { def = setDef, pawn = p };
            Field(set, typeof(Hediff), "severityInt", 2f);
            p.health.hediffSet.hediffs.Add(set);
        }
        return p;
    }

    private static Hediff_LiuAuraBuff BuffFixture(Pawn target, Pawn source, int ticks)
    {
        Hediff_LiuAuraBuff buff = new Hediff_LiuAuraBuff { def = auraDef, pawn = target };
        float severity = LiuSetUtility.IsTwoOfTwo(target) ? 4f : 1f;
        Field(buff, typeof(Hediff), "severityInt", severity);
        buff.comps = new List<HediffComp>
        {
            new HediffComp_LiuAuraLink { parent = buff, source = source, props = new HediffCompProperties_LiuAuraLink() },
            new HediffComp_Disappears { parent = buff, ticksToDisappear = ticks, disappearsAfterTicks = 35, props = new HediffCompProperties_Disappears { leaveFreshWounds = true } }
        };
        return buff;
    }

    private static float Mood(Pawn p) { ThoughtState state = readState(p); return state.Active ? thoughtDef.stages[state.StageIndex].baseMoodEffect : 0; }
    private static float Capacity(Pawn p)
    {
        float result = 0;
        foreach (Hediff h in p.health.hediffSet.hediffs) if (h.def == auraDef) result += h.CapMods[0].offset;
        return result;
    }
    private static void Expected(Pawn p, float mood, float capacity)
    {
        Check(Mood(p) == mood, "Mood mismatch: expected " + mood + ", actual " + Mood(p));
        Check(Math.Abs(Capacity(p) - capacity) < 0.00001f, "Capacity mismatch.");
    }

    private static bool SkipHealthState() { return false; }
    private static bool SkipRenderCaches() { cacheNotices++; return false; }
    private static bool CaptureFleck(FleckCreationData __0) { flecks.Add(__0); return false; }
    // 独立进程没有 Unity 原生旋转运行时，用平面角模拟该边界。
    private static bool SimulateAngle(IntVec3 __instance, ref float __result)
    {
        __result = (float)((Math.Atan2(__instance.x, __instance.z) * 180.0 / Math.PI + 360.0) % 360.0);
        return false;
    }

    public static void Run(string root)
    {
        // 测试进程手工提供本链路使用的 DefOf，不执行游戏资源加载。
        Field(null, typeof(DefOfHelper), "bindingNow", true);
        PawnKindDefOf.WildMan = new PawnKindDef();
        FleshTypeDefOf.Mechanoid = new FleshTypeDef { defName = "Mechanoid" };
        XmlDocument auraXml = Read(root, "Defs/HediffDefs/Hediffs_LiuSet.xml");
        XmlDocument moodXml = Read(root, "Defs/ThoughtDefs/Thoughts_Liu.xml");
        setDef = new HediffDef { defName = "RK_LiuSet", hediffClass = typeof(Hediff_LiuSet), maxSeverity = 2, initialSeverity = 1, stages = new List<HediffStage> { new HediffStage(), new HediffStage { minSeverity = 2 } } };
        XmlNode aura = auraXml.SelectSingleNode("/Defs/HediffDef[defName='RK_LiuAuraBuff']");
        auraDef = new HediffDef
        {
            defName = "RK_LiuAuraBuff", hediffClass = typeof(Hediff_LiuAuraBuff), initialSeverity = 1,
            maxSeverity = Number(aura["maxSeverity"].InnerText), stages = new List<HediffStage>(),
            comps = new List<HediffCompProperties>
            {
                new HediffCompProperties_LiuAuraLink(),
                new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(int.Parse(aura.SelectSingleNode("comps/li[@Class='HediffCompProperties_Disappears']/disappearsAfterTicks").InnerText), 35) }
            }
        };
        foreach (XmlNode stage in aura.SelectNodes("stages/li")) auraDef.stages.Add(new HediffStage { minSeverity = Number(stage["minSeverity"].InnerText), capMods = new List<PawnCapacityModifier> { new PawnCapacityModifier { offset = Number(stage.SelectSingleNode("capMods/li/offset").InnerText) } } });
        LiuSetDefOf.RK_LiuSet = setDef; LiuSetDefOf.RK_LiuAuraBuff = auraDef;
        thoughtDef = new ThoughtDef { hediff = auraDef, stages = new List<ThoughtStage>() };
        foreach (XmlNode stage in moodXml.SelectNodes("/Defs/ThoughtDef/stages/li")) thoughtDef.stages.Add(new ThoughtStage
        {
            label = stage["label"].InnerText, description = stage["description"].InnerText,
            baseMoodEffect = Number(stage["baseMoodEffect"].InnerText)
        });
        string name = moodXml.SelectSingleNode("/Defs/ThoughtDef/workerClass").InnerText;
        Type workerType = typeof(LiuSetUtility).Assembly.GetType(name);
        Check(workerType.BaseType == typeof(ThoughtWorker_Hediff), "Thought no longer inherits vanilla Hediff worker.");
        Check(workerType.GetMethod("CurrentStateInternal", BindingFlags.Instance | BindingFlags.NonPublic).DeclaringType == typeof(ThoughtWorker_Hediff), "Native stage decision was rewritten.");
        ThoughtWorker worker = (ThoughtWorker)Activator.CreateInstance(workerType); worker.def = thoughtDef;
        readState = (ReadState)Delegate.CreateDelegate(typeof(ReadState), worker, workerType.GetMethod("CurrentStateInternal", BindingFlags.Instance | BindingFlags.NonPublic));
        Current.Game = Raw<Game>(); Current.Game.tickManager = Raw<TickManager>();
        Current.Game.uniqueIDsManager = new UniqueIDsManager();
        Harmony harmony = new Harmony("XIYUNTE.IsolatedSimulation");
        harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "CheckForStateChange"), prefix: new HarmonyMethod(typeof(Simulation), "SkipHealthState"));
        harmony.Patch(AccessTools.Method(typeof(HediffSet), "DirtyCache"), prefix: new HarmonyMethod(typeof(Simulation), "SkipRenderCaches"));
        ChainOne(auraXml);
        ChainTwo();
        ChainThree(root, harmony);
        Say("PASS checks=" + checks + "; isolated boundaries: health state transition/render caches/Pawn DrawPos/Unity planar angle/Fleck submission.");
        Benchmark();
        File.WriteAllLines(Path.Combine(root, "Source/Verification/simulation-results.txt"), lines.ToArray());
    }

    private static void ChainOne(XmlDocument xml)
    {
        for (int full = 0; full < 2; full++)
        {
            Pawn p = PawnFixture(20, full == 1); Expected(p, 0, 0);
            Pawn source = PawnFixture(0, true);
            LiuSetUtility.GrantAuraFrom(p, source, 35);
            Hediff_LiuAuraBuff buff = (Hediff_LiuAuraBuff)p.health.hediffSet.GetFirstHediffOfDef(auraDef);
            Expected(p, full == 1 ? 6 : 2, full == 1 ? 0.06f : 0.02f);
            Check(buff.TryGetComp<HediffComp_LiuAuraLink>().source == source, "New aura source link failed.");
            int before = cacheNotices;
            for (int i = 0; i < 100; i++) LiuSetUtility.GrantAuraFrom(p, buff.TryGetComp<HediffComp_LiuAuraLink>().source, 35);
            Check(p.health.hediffSet.hediffs.Count == 21 + full, "Same source duplicated.");
            Check(buff.TryGetComp<HediffComp_Disappears>().ticksToDisappear == 35, "Source refresh failed.");
            Check(cacheNotices == before, "Source refresh invalidated render caches.");
        }
        XmlNode target = xml.SelectSingleNode("/Defs/HediffDef[defName='RK_LiuSet']/comps/li/targetingParameters");
        TargetingParameters parameters = new TargetingParameters { canTargetBuildings = false, canTargetAnimals = false, canTargetMechs = false, onlyTargetColonists = true };
        foreach (string field in new string[] { "canTargetBuildings", "canTargetAnimals", "canTargetMechs", "onlyTargetColonists" })
            Check(target[field].InnerText == typeof(TargetingParameters).GetField(field).GetValue(parameters).ToString().ToLowerInvariant(), "Target parameter mismatch: " + field);
        Faction player = new Faction { def = new FactionDef { isPlayer = true } };
        Pawn colonist = PawnFixture(0, false); Field(colonist, typeof(Thing), "factionInt", player);
        Check(parameters.CanTarget(colonist), "Vanilla command rejected player humanlike pawn.");
        Pawn visitor = PawnFixture(0, false); Field(visitor, typeof(Thing), "factionInt", new Faction { def = new FactionDef() });
        Check(!parameters.CanTarget(visitor), "Vanilla command accepted non-player visitor.");
        Pawn animal = PawnFixture(0, false); animal.def.race.intelligence = Intelligence.Animal; Field(animal, typeof(Thing), "factionInt", player);
        Check(!parameters.CanTarget(animal), "Vanilla command accepted allied animal.");
        Pawn mech = PawnFixture(0, false); mech.def.race.intelligence = Intelligence.ToolUser; Field(mech.def.race, typeof(RaceProperties), "fleshType", FleshTypeDefOf.Mechanoid); Field(mech, typeof(Thing), "factionInt", player);
        Check(!parameters.CanTarget(mech), "Vanilla command accepted allied mech.");
        Check(LiuSetUtility.InAuraRange(new IntVec3(0,0,0), new IntVec3(6,0,6), 6), "Square diagonal excluded.");
        Check(!LiuSetUtility.InAuraRange(new IntVec3(0,0,0), new IntVec3(7,0,0), 6), "Out of range accepted.");
        ThoughtWorker worker = new ThoughtWorker_LiuAura { def = thoughtDef };
        Pawn tooltipPawn = PawnFixture(0, true); tooltipPawn.health.AddHediff(BuffFixture(tooltipPawn, tooltipPawn, 35));
        for (int i = 0; i < thoughtDef.stages.Count; i++)
        {
            ThoughtStage stage = thoughtDef.stages[i];
            Check(!string.IsNullOrEmpty(stage.description), "Thought stage description is empty.");
            foreach (string error in stage.ConfigErrors()) Check(false, "Thought stage configuration error: " + error);
            string description = worker.PostProcessDescription(tooltipPawn, stage.description);
            Check(description == stage.description && description.IndexOf('\n') < 0 && description.IndexOf('\r') < 0, "Tooltip includes extra source paragraph or line breaks.");
            Check(stage.description == thoughtDef.stages[i < 3 ? 0 : 3].description, "Membership description differs across stack stages.");
        }
        Check(thoughtDef.stages[0].description != thoughtDef.stages[3].description, "Member and non-member descriptions are identical.");
        Say("CHAIN 1 PASS: full/non-full single source, 100 refreshes without duplication/cache invalidation; vanilla CanTarget accepts player humanlike pawn, rejects visitor/animal/mech; square boundaries; 6 valid stage descriptions, 2 membership texts, no added tooltip lines; native stage decision inherited.");
    }

    private static void ChainTwo()
    {
        Pawn p = PawnFixture(20, true);
        List<Hediff_LiuAuraBuff> buffs = new List<Hediff_LiuAuraBuff>();
        float[] moods = { 6, 12, 18, 18 };
        for (int i = 0; i < 4; i++)
        {
            var buff = BuffFixture(p, PawnFixture(0, true), 35); buffs.Add(buff);
            int before = cacheNotices; p.health.AddHediff(buff); Expected(p, moods[i], (i + 1) * 0.06f);
            Check(cacheNotices - before <= 2, "Source addition invalidated every instance.");
        }
        LiuSetUtility.RemovePiece(p); Expected(p, 6, 0.08f);
        LiuSetUtility.AddPiece(p); Expected(p, 18, 0.24f);
        p.health.RemoveHediff(buffs[0]); Expected(p, 18, 0.18f);
        float adjust = 0;
        var expiry = buffs[1].TryGetComp<HediffComp_Disappears>();
        for (int i = 0; i < 35; i++) expiry.CompPostTick(ref adjust);
        Check(expiry.CompShouldRemove, "Native expiry did not trigger after 35 ticks.");
        p.health.RemoveHediff(buffs[1]); Expected(p, 12, 0.12f);
        p.health.RemoveHediff(buffs[2]); Expected(p, 6, 0.06f);
        p.health.RemoveHediff(buffs[3]); Expected(p, 0, 0);
        Pawn nonFull = PawnFixture(20, false);
        for (int i = 0; i < 4; i++)
        {
            nonFull.health.AddHediff(BuffFixture(nonFull, PawnFixture(0, true), 35));
            Expected(nonFull, Math.Min(i + 1, 3) * 2, (i + 1) * 0.02f);
        }
        Say("CHAIN 2 values: full mood=6/12/18/18, consciousness offsets=6/12/18/24%; non-full mood=2/4/6/6, offsets=2/4/6/8%.");
        Say("CHAIN 2 PASS: 1/2/3/4 sources, mood cap with unlimited consciousness, unequip/re-equip, first-source removal, native 35-tick expiry, final removal.");
    }

    private static void ChainThree(string root, Harmony harmony)
    {
        XmlDocument cinq = Read(root, "Defs/EffecterDefs/Cinq_AttackEffect.xml");
        XmlDocument box = Read(root, "Defs/EffecterDefs/DeliveryBox_AttackEffect.xml");
        foreach (XmlNode boxEffect in box.SelectNodes("/Defs/EffecterDef"))
        {
            foreach (string field in new string[] { "subEffecterClass", "burstCount", "spawnLocType", "rotation" })
                Check(cinq.SelectSingleNode("/Defs/EffecterDef/children/li/" + field).InnerText == boxEffect.SelectSingleNode("children/li/" + field).InnerText, "VFX differs: " + field);
            Check(boxEffect["offsetTowardsTarget"].InnerText == "0.9~0.9", "DeliveryBox offset differs.");
        }
        Check(cinq.SelectSingleNode("/Defs/FleckDef").Attributes["ParentName"].Value == "RK_DeliveryBox_AttackFleckBase", "Fleck parent differs.");
        Check(cinq.SelectSingleNode("//perRotationOffsets") == null, "Four-way offset used.");
        harmony.Patch(AccessTools.PropertyGetter(typeof(IntVec3), "AngleFlat"), prefix: new HarmonyMethod(typeof(Simulation), "SimulateAngle"));
        harmony.Patch(AccessTools.Method(typeof(FleckManager), "CreateFleck"), prefix: new HarmonyMethod(typeof(Simulation), "CaptureFleck"));
        Map map = Raw<Map>(); map.info = new MapInfo { Size = new IntVec3(100, 1, 100) }; map.flecks = Raw<FleckManager>();
        Field(Current.Game, typeof(Game), "maps", new List<Map> { map }); Current.Game.currentMapIndex = 0;
        SimulationPawn caster = Raw<SimulationPawn>();
        Field(caster, typeof(Thing), "mapIndexOrState", (sbyte)0);
        Field(caster, typeof(Thing), "positionInt", new IntVec3(50, 0, 50));
        Action<Verb_MeleeAttack> attackEffect = (Action<Verb_MeleeAttack>)Delegate.CreateDelegate(typeof(Action<Verb_MeleeAttack>),
            typeof(LiuSetUtility).Assembly.GetType("XIYUNTE.Patch_Verb_MeleeAttack_DeliveryBoxMultiAttack").GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
        XmlDocument weaponXml = Read(root, "Defs/ThingDefs_Items/Weapon_cinq.xml");
        Check(weaponXml.SelectSingleNode("//modExtensions/li/attackEffecter").InnerText == "RK_Cinq_AttackEffect", "Cinq extension not wired.");
        EffecterDef effect = EffectFixture(cinq.SelectSingleNode("/Defs/EffecterDef"));
        Check(effect.offsetTowardsTarget.min == effect.offsetTowardsTarget.max && effect.offsetTowardsTarget.min > 0, "Cinq offset must use a fixed positive distance.");
        Check(effect.children[0].scale.min == effect.children[0].scale.max, "Cinq scale must use a fixed value.");
        ThingWithComps weapon = Raw<ThingWithComps>(); weapon.def = Raw<ThingDef>();
        weapon.def.modExtensions = new List<DefModExtension> { new MeleeAttackEffectExtension { attackEffecter = effect } };
        CompEquippable equippable = new CompEquippable { parent = weapon };
        Verb_MeleeAttack verb = Raw<Verb_MeleeAttackDamage>(); verb.caster = caster; verb.verbTracker = new VerbTracker(equippable);
        int[,] directions = { {0,1}, {1,1}, {1,0}, {1,-1}, {0,-1}, {-1,-1}, {-1,0}, {-1,1} };
        for (int i = 0; i < 8; i++)
        {
            IntVec3 direction = new IntVec3(directions[i,0], 0, directions[i,1]);
            flecks.Clear(); Field(verb, typeof(Verb), "currentTarget", new LocalTargetInfo(caster.Position + direction)); attackEffect(verb);
            Check(flecks.Count == 1, "Eight-way Fleck count differs.");
            Check(flecks[0].def == effect.children[0].fleckDef && flecks[0].scale == effect.children[0].scale.min, "Cinq Fleck or XML scale differs.");
            Vector3 offset = flecks[0].spawnPosition - caster.DrawPos;
            Check(Math.Abs(offset.magnitude - effect.offsetTowardsTarget.min) < 0.00001f, "Eight-way XML offset length differs.");
            Check((offset - direction.ToVector3().normalized * effect.offsetTowardsTarget.min).magnitude < 0.00001f, "Eight-way offset direction differs.");
            Check(Math.Abs((flecks[0].rotation + 450f) % 360f - i * 45f) < 0.0001f, "Eight-way simulated angle differs.");
        }
        Box_shift transformerProps = new Box_shift();
        foreach (XmlNode mode in Read(root, "Defs/ThingDefs_Items/Weapon_Box.xml").SelectNodes("//modes/li"))
        {
            XmlNode name = mode["attackEffecter"];
            transformerProps.modes.Add(new WeaponMode { attackEffecter = name == null ? null : EffectFixture(box.SelectSingleNode("/Defs/EffecterDef[defName='" + name.InnerText + "']")) });
        }
        CompWeaponTransformer transformer = new CompWeaponTransformer { parent = weapon, props = transformerProps };
        Field(weapon, typeof(ThingWithComps), "comps", new List<ThingComp> { transformer });
        for (int i = 0; i < transformerProps.modes.Count; i++)
        {
            Field(transformer, typeof(CompWeaponTransformer), "modeIndex", i); flecks.Clear(); attackEffect(verb);
            Check(flecks.Count == (i == 0 ? 0 : 1), "DeliveryBox original mode path changed.");
            if (i > 0) Check(flecks[0].def == transformer.CurrentMode.attackEffecter.children[0].fleckDef, "DeliveryBox selected wrong effect.");
        }
        Field(weapon, typeof(ThingWithComps), "comps", null); weapon.def.modExtensions = null;
        flecks.Clear(); attackEffect(verb); Check(flecks.Count == 0, "Ordinary weapon played a custom effect.");
        weapon.def.modExtensions = new List<DefModExtension> { new MeleeAttackEffectExtension { attackEffecter = effect } };
        Measure("Cinq Postfix -> vanilla Effecter -> captured Fleck (render/angle boundaries simulated)", delegate { flecks.Clear(); attackEffect(verb); }, 30000);
        HediffDef zwei = new HediffDef { defName = "zwei_AuraBuff", stages = new List<HediffStage> { new HediffStage() } };
        Pawn target = PawnFixture(0, false);
        var unrelated = new Hediff_LiuAuraBuff { def = zwei, pawn = target }; Field(unrelated, typeof(Hediff), "severityInt", 1f);
        target.health.AddHediff(unrelated); target.health.RemoveHediff(unrelated);
        Expected(target, 0, 0);
        Say("CHAIN 3 PASS: production melee Postfix -> vanilla Effecter -> Sprayer -> captured Fleck; Cinq eight directions, DeliveryBox 5 modes, ordinary weapon, Zwei isolation. Unity planar angle/DrawPos/Fleck renderer are simulated boundaries.");
    }

    private static EffecterDef EffectFixture(XmlNode node)
    {
        XmlNode child = node.SelectSingleNode("children/li");
        return new EffecterDef
        {
            defName = node["defName"].InnerText, offsetTowardsTarget = Range(node["offsetTowardsTarget"].InnerText),
            children = new List<SubEffecterDef> { new SubEffecterDef
            {
                subEffecterClass = typeof(Effecter).Assembly.GetType("Verse." + child["subEffecterClass"].InnerText),
                burstCount = new IntRange(int.Parse(child["burstCount"].InnerText), int.Parse(child["burstCount"].InnerText)),
                scale = Range(child["scale"].InnerText), spawnLocType = (MoteSpawnLocType)Enum.Parse(typeof(MoteSpawnLocType), child["spawnLocType"].InnerText),
                rotation = Range(child["rotation"].InnerText),
                fleckDef = new FleckDef { defName = child["fleckDef"].InnerText, drawOffscreen = true }
            } }
        };
    }

    private static FloatRange Range(string value) { string[] bounds = value.Split('~'); return new FloatRange(Number(bounds[0]), Number(bounds[1])); }

    private static void Benchmark()
    {
        const int iterations = 300000;
        foreach (int sources in new int[] { 1, 3, 20 })
        {
            Pawn p = PawnFixture(25, true);
            for (int i = 0; i < sources; i++) p.health.hediffSet.hediffs.Add(BuffFixture(p, PawnFixture(0, true), 35));
            LiuSetUtility.SyncAuraStages(p);
            for (int i = 0; i < 10000; i++) readState(p);
            long[] samples = new long[5]; int gc = GC.CollectionCount(0); int sum = 0; long bytes = 0;
            for (int sample = 0; sample < samples.Length; sample++)
            {
                Stopwatch timer = Stopwatch.StartNew();
                long startBytes = allocatedBytes();
                for (int i = 0; i < iterations; i++) sum += readState(p).StageIndex;
                bytes += allocatedBytes() - startBytes;
                timer.Stop(); samples[sample] = timer.ElapsedTicks;
            }
            Array.Sort(samples);
            double ns = samples[2] * 1000000000.0 / Stopwatch.Frequency / iterations;
            Say("VANILLA mood query sources=" + sources + ", unrelated=25, median_ns=" + ns.ToString("F2", CultureInfo.InvariantCulture) + ", bytes_per_call=" + (bytes / (double)(iterations * samples.Length)).ToString("F2", CultureInfo.InvariantCulture) + ", Gen0_collections=" + (GC.CollectionCount(0) - gc) + ", checksum=" + sum);
            Measure("stage sync unchanged sources=" + sources, delegate { LiuSetUtility.SyncAuraStages(p); }, iterations);
            Pawn source = ((HediffWithComps)p.health.hediffSet.GetFirstHediffOfDef(auraDef)).TryGetComp<HediffComp_LiuAuraLink>().source;
            Measure("existing-source refresh sources=" + sources, delegate { LiuSetUtility.GrantAuraFrom(p, source, 35); }, iterations);
        }
    }

    private static void Measure(string name, Action action, int iterations)
    {
        for (int i = 0; i < 10000; i++) action();
        long[] samples = new long[5]; long bytes = 0;
        for (int sample = 0; sample < samples.Length; sample++)
        {
            Stopwatch timer = Stopwatch.StartNew(); long startBytes = allocatedBytes();
            for (int i = 0; i < iterations; i++) action();
            bytes += allocatedBytes() - startBytes; timer.Stop(); samples[sample] = timer.ElapsedTicks;
        }
        Array.Sort(samples);
        double ns = samples[2] * 1000000000.0 / Stopwatch.Frequency / iterations;
        Say("MANAGED " + name + ", median_ns=" + ns.ToString("F2", CultureInfo.InvariantCulture) + ", bytes_per_call=" + (bytes / (double)(iterations * samples.Length)).ToString("F2", CultureInfo.InvariantCulture));
    }
}
