using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using RimWorld;
using Verse;
using XIYUNTE.SpecialCharacter;

public static class SpecialCharacterVerification
{
    private static int checks;

    public static int Main(string[] args)
    {
        string modRoot = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string dir in new[] { "Assemblies", "Source/ClassLibrary1/bin/Debug" })
            {
                string file = Path.Combine(modRoot, dir, name);
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try { return Run(modRoot); }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static int Run(string modRoot)
    {
        // 独立验证进程未初始化游戏 Prefs，停用与解析逻辑无关的性能采样。
        DeepProfiler.enabled = false;
        string output = Path.Combine(modRoot, "Source/Verification/bin");
        DispatchLifecycle order = new DispatchLifecycle { arrivalTick = 2500 };
        Check(!order.IsDue(2499), "未到派遣时间不提交");
        Check(order.IsDue(2500), "到期允许提交");
        order = RoundTrip(order, Path.Combine(output, "SpecialCharacter-waiting.xml"));
        Check(order.stage == DispatchStage.Waiting && order.arrivalTick == 2500,
            "XML 快照恢复待派遣时间和状态");
        order.Launch();
        order = RoundTrip(order, Path.Combine(output, "SpecialCharacter-inflight.xml"));
        Check(order.stage == DispatchStage.InFlight && !order.arrivalLetterSent,
            "XML 快照恢复飞行状态");
        Check(order.Complete(), "首次抵达提交一次通知");
        Check(!order.Complete(), "重复完成不能提交第二次通知");
        order = RoundTrip(order, Path.Combine(output, "SpecialCharacter-arrived.xml"));
        Check(order.stage == DispatchStage.Arrived && order.arrivalLetterSent,
            "XML 快照恢复已完成和通知标记");
        for (int i = 0; i < 30; i++)
            Check(!order.Complete(), "完成后重复事件不提交通知 " + (i + 1));
        Check(!order.IsDue(1000000), "完成后时间推进不重新派遣");
        bool rejected = false;
        try { order.Launch(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "已完成订单不能重新发起派遣");

        DispatchLifecycle failed = new DispatchLifecycle { arrivalTick = 2500 };
        failed.Fail();
        failed = RoundTrip(failed, Path.Combine(output, "SpecialCharacter-failed.xml"));
        Check(failed.stage == DispatchStage.Failed && !failed.IsDue(1000000),
            "失败状态恢复后不自动循环重试");
        rejected = false;
        try { failed.Complete(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "失败订单不能发出到达通知");
        VerifyDeferredLoadInitialization();
        VerifyTraitRegression();
        VerifySilverSplit(500, 500);
        VerifySilverSplit(500, 175);
        VerifyDefFields(modRoot, output);
        Console.WriteLine("PASS: " + checks + " checks; compiled lifecycle, early load initialization, XML snapshots, TraitRequirement custom loader, native silver splitting and Def field metadata.");
        Console.WriteLine("Scope: transitions, notification eligibility, unresolved-reference initialization, trait reference registration and field metadata; live Scribe, map, pawn generation and GUI are outside this automated run.");
        return 0;
    }

    // 原版拆堆与容器操作在独立进程验证；地图网格、Spawn 和物品 ID 分配不在本例范围。
    public sealed class VerificationSilver : Thing
    {
        public override void PostMake() { }
    }

    // 容器通知需要游戏 DefOf 和 UI；本例保留原版接收、拆堆及归属变更，隔离游戏通知。
    private sealed class VerificationOwner : ThingOwner<Thing>
    {
        public VerificationOwner(IThingHolder owner) : base(owner) { }
        protected override void NotifyAdded(Thing item) { }
        protected override void NotifyRemoved(Thing item) { }
    }

    private static void VerifySilverSplit(int count, int amount)
    {
        Map map = (Map)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Map));
        ThingOwner<Thing> mapThings = new VerificationOwner(map);
        ThingDef silverDef = (ThingDef)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ThingDef));
        silverDef.defName = "VerificationSilver";
        silverDef.thingClass = typeof(VerificationSilver);
        silverDef.stackLimit = 500;
        Thing stack = new VerificationSilver
        {
            def = silverDef,
            stackCount = count
        };
        Check(mapThings.TryAdd(stack, canMergeWithExistingStacks: false), "银堆由地图容器持有");
        Thing deducted = stack.SplitOff(amount);
        Check(deducted.holdingOwner == null && deducted.stackCount == amount, "原版拆堆解除被扣金额的容器归属：" + amount);
        SpecialCharacterOrder order = new SpecialCharacterOrder();
        order.payment = new VerificationOwner(order);
        Check(order.payment.TryAdd(deducted, canMergeWithExistingStacks: false), "订单接收拆下的银堆：" + amount);
        Check(mapThings.Sum(t => t.stackCount) == count - amount && order.payment.Sum(t => t.stackCount) == amount,
            "地图余款和订单金额守恒：" + amount);
    }

    private static void VerifyDefFields(string modRoot, string output)
    {
        XmlDocument document = new XmlDocument();
        document.Load(Path.Combine(modRoot, "Defs/SpecialCharacter/SpecialCharacters.xml"));
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> references = new HashSet<string>(StringComparer.Ordinal);
        foreach (XmlElement node in document.SelectNodes("/Defs/*"))
        {
            Type type = node.Name == "PawnKindDef" ? typeof(PawnKindDef) :
                node.Name == "ThingDef" ? typeof(ThingDef) : typeof(SpecialCharacterDef).Assembly.GetType(node.Name, true);
            Check(typeof(Def).IsAssignableFrom(type), "XML 根类型是可加载 Def：" + node.Name);
            string name = node["defName"].InnerText;
            Check(System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9_]+$") && names.Add(name),
                "特殊人物 Def 名称合法且无重复：" + name);
            VerifyFields(node, type, references);
        }
        Type podType = typeof(SpecialCharacterDef).Assembly.GetType(document.SelectSingleNode("/Defs/ThingDef/thingClass").InnerText, true);
        Check(typeof(RimWorld.ActiveTransporter).IsAssignableFrom(podType), "派遣舱 thingClass 存在且继承原版载体");
        File.WriteAllLines(Path.Combine(output, "SpecialCharacter-def-references.tsv"), references.OrderBy(x => x));
    }

    private static void VerifyFields(XmlElement node, Type type, HashSet<string> references)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            foreach (XmlElement child in node.SelectNodes("*")) VerifyFields(child, type.GetGenericArguments()[0], references);
            return;
        }
        if (type == typeof(TraitRequirement))
        {
            TraitRequirement trait;
            string name = ReadTraitReference(node, out trait);
            Check(name == node.Name, "固定特质的原版引用名称与节点名称一致：" + name);
            Check(trait.degree.HasValue, "固定特质的原版解析结果包含等级：" + name);
            Check(trait.degree == int.Parse(node.InnerText), "固定特质的原版解析等级与配置一致：" + name);
            references.Add("TraitDef\t" + name);
            return;
        }
        if (type.GetMethod("LoadDataFromXmlCustom") != null)
            throw new InvalidOperationException("自定义 XML 解析类型必须单独核验：" + type.Name);
        if (typeof(Def).IsAssignableFrom(type) && !node.HasChildNodes) return;
        if (typeof(Def).IsAssignableFrom(type) && node.SelectNodes("*").Count == 0)
        {
            if (node.InnerText.Length > 0) references.Add(type.Name + "\t" + node.InnerText);
            return;
        }
        if (type.IsEnum)
        {
            Check(Enum.IsDefined(type, node.InnerText), "枚举字段可解析：" + node.Name);
            return;
        }
        HashSet<string> fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (XmlElement child in node.SelectNodes("*"))
        {
            FieldInfo field = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.Name == child.Name || f.GetCustomAttributes(typeof(LoadAliasAttribute), true)
                    .Cast<LoadAliasAttribute>().Any(a => string.Equals(a.alias, child.Name, StringComparison.OrdinalIgnoreCase)));
            Check(field != null && fields.Add(child.Name), "XML 字段存在且未重复：" + type.Name + "." + child.Name);
            VerifyFields(child, field.FieldType, references);
        }
    }

    private static string ReadTraitReference(XmlElement node, out TraitRequirement trait)
    {
        // 执行原版自定义解析入口并读取其登记的引用，不用字段反射模拟 XML 解析。
        System.Collections.IList wanted = (System.Collections.IList)typeof(DirectXmlCrossRefLoader)
            .GetField("wantedRefs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        int before = wanted.Count;
        trait = new TraitRequirement();
        trait.LoadDataFromXmlCustom(node);
        Check(wanted.Count == before + 1, "固定特质只登记一个 Def 引用");
        object reference = wanted[before];
        Type type = reference.GetType();
        FieldInfo field = (FieldInfo)type.GetField("fi", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reference);
        Check(field.DeclaringType == typeof(TraitRequirement) && field.Name == "def" && field.FieldType == typeof(TraitDef),
            "原版解析登记 TraitRequirement.def 引用");
        string name = (string)type.GetField("defName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reference);
        wanted.RemoveAt(before);
        return name;
    }

    private static void VerifyTraitRegression()
    {
        foreach (string entry in new[] { "Beauty|2", "Industriousness|1", "Kind|0" })
        {
            string[] parts = entry.Split('|');
            XmlDocument old = new XmlDocument();
            old.LoadXml("<li><def>" + parts[0] + "</def><degree>" + parts[1] + "</degree></li>");
            TraitRequirement trait;
            string name = ReadTraitReference(old.DocumentElement, out trait);
            Check(name == parts[0] + parts[1] && !trait.degree.HasValue,
                "复现旧写法合并名称与等级，且无法读取 degree：" + parts[0]);
        }
    }

    private static void VerifyDeferredLoadInitialization()
    {
        WorldComponent_SpecialCharacters service = new WorldComponent_SpecialCharacters(null);
        SpecialCharacterDef character = new SpecialCharacterDef { defName = "VerificationCharacter" };
        SpecialCharacterOrder pending = new SpecialCharacterOrder
        {
            character = character,
            lifecycle = new DispatchLifecycle { arrivalTick = 2500 }
        };
        FieldInfo ordersField = typeof(WorldComponent_SpecialCharacters).GetField("orders", BindingFlags.Instance | BindingFlags.NonPublic);
        ((List<SpecialCharacterOrder>)ordersField.GetValue(service)).Add(pending);
        // Game.LoadGame 在地图和交叉引用加载前调用世界初始化。
        service.FinalizeInit(fromLoad: true);
        Check(service.GetOrder(character) == pending, "世界读档早期初始化保留同一订单");
        Check(pending.lifecycle.stage == DispatchStage.Waiting && pending.failure == null,
            "尚未恢复的 Pawn 和地图引用不导致提前失败");
        Check(!pending.lifecycle.arrivalLetterSent && pending.payment.Count == 0,
            "早期初始化不提交到达通知或修改款项");
    }

    private static DispatchLifecycle RoundTrip(DispatchLifecycle order, string file)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(DispatchLifecycle));
        using (StreamWriter writer = new StreamWriter(file)) serializer.Serialize(writer, order);
        DispatchLifecycle restored;
        using (StreamReader reader = new StreamReader(file)) restored = (DispatchLifecycle)serializer.Deserialize(reader);
        Check(restored != null, "状态 XML 快照往返成功");
        return restored;
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("FAIL: " + name);
        checks++;
    }
}
