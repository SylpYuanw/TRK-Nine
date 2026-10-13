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
        try { return Run(modRoot, args.Length > 1 ? args[1] : null); }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static int Run(string modRoot, string facialAnimationAssembly)
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
        VerifyEmptyLoadedStorage();
        VerifyTraitRegression();
        VerifySilverSplit(500, 500);
        VerifySilverSplit(500, 175);
        VerifyCharacterIdentity();
        VerifyAppearanceOrder();
        VerifyDefFields(modRoot, output);
        VerifyInitialOldInjuries(modRoot);
        VerifyOptionalHairPatch(modRoot);
        VerifyFacialAnimation(modRoot, facialAnimationAssembly);
        Console.WriteLine("PASS: " + checks + " checks; compiled lifecycle, early load initialization, character identity/ownership transfer, appearance call order, XML snapshots, TraitRequirement custom loader, native silver splitting and Def field metadata.");
        Console.WriteLine("Scope: transitions, notification eligibility, empty component native Scribe loading, identity/ownership, trait reference registration and field metadata; complete pawn Scribe round trips, maps, generation and GUI are outside this automated run.");
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
        document.Load(Path.Combine(modRoot, "Defs/SpecialCharacter/SpecialCharacterPod.xml"));
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> references = new HashSet<string>(StringComparer.Ordinal);
        IEnumerable<XmlElement> nodes = Directory.GetFiles(Path.Combine(modRoot, "Defs/SpecialCharacter"), "*.xml")
            .SelectMany(file =>
            {
                XmlDocument definitions = new XmlDocument();
                definitions.Load(file);
                return definitions.SelectNodes("/Defs/*").Cast<XmlElement>();
            });
        foreach (XmlElement node in nodes)
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

    // 执行原版旧伤入口，验证各人物模板在生成阶段关闭随机伤病。
    private static void VerifyInitialOldInjuries(string modRoot)
    {
        int templates = 0;
        foreach (string file in Directory.GetFiles(Path.Combine(modRoot, "Defs/SpecialCharacter"), "*.xml"))
        {
            XmlDocument defs = new XmlDocument();
            defs.Load(file);
            foreach (XmlElement node in defs.SelectNodes("/Defs/PawnKindDef"))
            {
                string name = node["defName"].InnerText;
                Check(node["allowOldAgeInjuries"] != null, "人物模板明确配置初始旧伤开关：" + name);
                XmlDocument settings = new XmlDocument();
                settings.LoadXml("<PawnKindDef />");
                settings.DocumentElement.AppendChild(settings.ImportNode(node["allowOldAgeInjuries"], true));
                PawnKindDef kind = DirectXmlToObject.ObjectFromXml<PawnKindDef>(settings.DocumentElement, false);
                Check(!kind.allowOldAgeInjuries, "原版 XML 解析关闭初始随机旧伤：" + name);
                Pawn pawn = new Pawn { kindDef = kind };
                // 未建立年龄、种族和健康组件，旧伤入口必须在访问这些对象前退出。
                AgeInjuryUtility.GenerateRandomOldAgeInjuries(pawn, true);
                Check(pawn.ageTracker == null && pawn.health == null,
                    "原版旧伤入口直接退出，不初始化或清理健康状态：" + name);
                templates++;
            }
        }
        Check(templates == 5, "五位特殊人物均覆盖初始旧伤限制");
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

    // 执行原版 LoadingVars/PostLoadInit，覆盖旧测试档的空节点和首次联络初始化。
    private static void VerifyEmptyLoadedStorage()
    {
        foreach (string fields in new[]
        {
            "<waitingPods><innerList /></waitingPods><orders />",
            "<pendingContents IsNull=\"True\" /><orders />",
            "<pendingContents><innerList /></pendingContents><orders />",
            ""
        })
        {
            WorldComponent_SpecialCharacters service = new WorldComponent_SpecialCharacters(null);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml("<li>" + fields + "</li>");
            XmlNode previousXml = Scribe.loader.curXmlParent;
            IExposable previousParent = Scribe.loader.curParent;
            LoadSaveMode previousMode = Scribe.mode;
            try
            {
                Scribe.loader.curXmlParent = xml.DocumentElement;
                Scribe.loader.curParent = service;
                Scribe.mode = LoadSaveMode.LoadingVars;
                service.ExposeData();
                Scribe.mode = LoadSaveMode.PostLoadInit;
                service.ExposeData();
            }
            finally
            {
                Scribe.loader.curXmlParent = previousXml;
                Scribe.loader.curParent = previousParent;
                Scribe.mode = previousMode;
            }
            Check(service.GetOrder(new SpecialCharacterDef()) == null, "原版读档恢复无订单状态，不创建人物");
            service.InitializeForContact();
            ThingOwner first = service.GetDirectlyHeldThings();
            service.InitializeForContact();
            Check(first != null && first.Count == 0 && ReferenceEquals(first, service.GetDirectlyHeldThings()),
                "首次联络建立空容器，多次联络复用同一容器");
        }
    }

    // 使用实际容器内的 PawnKind 查询和 ThingOwner 转移，隔离场景生成、UI 通知与 Unity 渲染。
    private static void VerifyCharacterIdentity()
    {
        WorldComponent_SpecialCharacters service = new WorldComponent_SpecialCharacters(null);
        SpecialCharacterDef character = new SpecialCharacterDef
        {
            defName = "VerificationCharacter", pawnKind = new PawnKindDef { defName = "VerificationKind" }
        };
        ThingDef pawnDef = (ThingDef)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "VerificationPawn";
        pawnDef.stackLimit = 1;
        Pawn pawn = new Pawn { def = pawnDef, kindDef = character.pawnKind };
        FieldInfo storageField = typeof(WorldComponent_SpecialCharacters).GetField("pendingContents", BindingFlags.Instance | BindingFlags.NonPublic);
        ThingOwner<Thing> storage = new VerificationOwner(service);
        storageField.SetValue(service, storage);
        Check(storage.TryAdd(pawn, canMergeWithExistingStacks: false), "实际人物进入世界组件容器");
        for (int i = 0; i < 30; i++)
        {
            Check(ReferenceEquals(service.GetCharacter(character, null), pawn), "反复查看返回同一实际 Pawn " + (i + 1));
            Check(pawn.holdingOwner == storage && storage.Count == 1, "查看不销毁或释放实际 Pawn " + (i + 1));
        }
        SpecialCharacterOrder order = new SpecialCharacterOrder { character = character, pawn = pawn };
        ThingOwner<Thing> delivery = new VerificationOwner(order);
        Check(delivery.TryAddOrTransfer(pawn, canMergeWithExistingStacks: false), "购买转移现有人物，无克隆或重建");
        Check(storage.Count == 0 && delivery.Count == 1 && delivery.Contains(pawn) && pawn.holdingOwner == delivery,
            "人物只有一个深度容器所有者");
        ((List<SpecialCharacterOrder>)typeof(WorldComponent_SpecialCharacters)
            .GetField("orders", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service)).Add(order);
        Check(ReferenceEquals(service.GetCharacter(character, null), pawn), "购买后详情继续返回订单中的同一 Pawn");
        order.pawn = null;
        order.lifecycle.Fail();
        Check(service.GetCharacter(character, null) == null && storage.Count == 0,
            "失败终态不创建替代人物");
    }

    // HAR 的首次通道访问会写回 story 发色；独立环境只验证实际 DLL 的调用顺序及肤色写入边界。
    private static void VerifyAppearanceOrder()
    {
        MethodInfo appearance = typeof(SpecialCharacterGenerator).GetMethod("ApplyAppearance", BindingFlags.NonPublic | BindingFlags.Static);
        List<MemberInfo> members = ReadILMembers(appearance);
        int channelWrite = members.FindIndex(m => m.Name == "Invoke");
        int hairWrite = members.FindIndex(m => m.DeclaringType == typeof(Pawn_StoryTracker) && m.Name == "set_HairColor");
        Check(channelWrite >= 0 && hairWrite > channelWrite, "固定 story 发色在 HAR 通道初始化和写入之后设置");
        Check(!members.Any(m => m.DeclaringType == typeof(Pawn_StoryTracker) &&
            (m.Name == "skinColorOverride" || m.Name == "set_SkinColorBase")), "人物初始化不覆盖原版基因肤色");
        List<MemberInfo> saving = ReadILMembers(typeof(WorldComponent_SpecialCharacters).GetMethod("ExposeData"));
        Check(!saving.OfType<MethodInfo>().Any(m => m.DeclaringType == typeof(Scribe_Collections) &&
            m.Name == "Look" && m.GetParameters()[0].ParameterType.GetElementType().IsGenericType &&
            m.GetParameters()[0].ParameterType.GetElementType().GetGenericTypeDefinition() == typeof(Dictionary<,>)),
            "人物只保存在容器内，不再保存重复的 Def/Pawn 字典");
    }

    // 门控读取 IL 元数据；传入实际 DLL 后执行配置写入并验证原控制器初始化保留固定值。
    private static void VerifyFacialAnimation(string modRoot, string assemblyPath)
    {
        XmlDocument xml = new XmlDocument();
        xml.Load(Path.Combine(modRoot, "Defs/SpecialCharacter/ManTianXing.xml"));
        SpecialCharacterFacialAnimation profile = DirectXmlToObject.ObjectFromXml<SpecialCharacterFacialAnimation>(
            xml.SelectSingleNode("/Defs/XIYUNTE.SpecialCharacter.SpecialCharacterDef/facialAnimation"), false);
        Check(!typeof(SpecialCharacterFacialAnimation).Assembly.GetReferencedAssemblies()
                .Any(a => a.Name == "FacialAnimation"), "主程序集不依赖可选 FA DLL");
        List<MemberInfo> gate = ReadILMembers(typeof(SpecialCharacterFacialAnimation).GetMethod("Apply"));
        Check(gate.Count(m => m.DeclaringType == typeof(ModsConfig) && m.Name == "IsActive") == 2 &&
            gate.Last().Name == "ApplyConfiguration", "两项模组检查先于 FA 配置读取");
        MethodInfo apply = typeof(SpecialCharacterFacialAnimation).GetMethod("ApplyConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
        if (string.IsNullOrEmpty(assemblyPath)) return;
        Assembly fa = Assembly.LoadFrom(assemblyPath);
        string[] controllers = { "Head", "Eyeball", "Lid", "Brow", "Mouth", "Skin" };
        string[] names = { profile.head, profile.eyes, profile.lids, profile.brows, profile.mouth, profile.skin };
        Pawn pawn = new Pawn { gender = Gender.Female };
        List<ThingComp> comps = new List<ThingComp>();
        List<Def> configured = new List<Def>();
        for (int i = 0; i < controllers.Length; i++)
        {
            Type type = fa.GetType("FacialAnimation." + controllers[i] + "ControllerComp", true);
            Type shapeType = type.BaseType.GetGenericArguments()[1];
            Def shape = (Def)Activator.CreateInstance(shapeType);
            shape.defName = "normal";
            AddVerificationDef(shapeType, shape);
            Type faceType = type.GetProperty("FaceType").PropertyType;
            Def face = (Def)Activator.CreateInstance(faceType);
            face.defName = names[i];
            AddVerificationDef(faceType, face);
            configured.Add(face);
            ThingComp comp = (ThingComp)Activator.CreateInstance(type);
            comp.parent = pawn;
            comps.Add(comp);
        }
        // 实际加载的鼠族存在重复控制器，渲染入口使用原版 GetComp 选择首个组件。
        foreach (string controller in controllers)
        {
            ThingComp duplicate = (ThingComp)Activator.CreateInstance(fa.GetType("FacialAnimation." + controller + "ControllerComp", true));
            duplicate.parent = pawn;
            comps.Add(duplicate);
        }
        typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn, comps);
        apply.Invoke(profile, new object[] { pawn });
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                Type type = comps[i].GetType();
                type.GetMethod("InitializeIfNeed").Invoke(comps[i], null);
                Check(ReferenceEquals(type.GetProperty("FaceType").GetValue(comps[i], null), configured[i]),
                    "FA 实际控制器初始化保留指定类型：" + names[i]);
            }
            Type eyeType = comps[1].GetType();
            Check(eyeType.GetProperty("FaceColor").GetValue(comps[1], null)
                .Equals(typeof(SpecialCharacterFacialAnimation).GetField("eyeColor").GetValue(profile)),
                "FA 实际控制器初始化保留主眼色");
            Check(eyeType.GetProperty("FaceSecondColor").GetValue(comps[1], null)
                .Equals(typeof(SpecialCharacterFacialAnimation).GetField("eyeColorSecond").GetValue(profile)),
                "FA 实际控制器初始化保留副眼色");
        }
        string head = profile.head;
        profile.head = "MissingFixedFace";
        bool rejected = false;
        try { apply.Invoke(profile, new object[] { pawn }); }
        catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
        profile.head = head;
        Check(rejected, "缺失 FA Def 明确失败，不替换为随机面部");
        Console.WriteLine("PASS: 实际 FA 1.6 重复控制器下使用原版 GetComp，六项类型和两项眼色初始化保留；模组门控按 IL 检查，渲染和完整 Pawn Scribe 往返不在此验证范围。");
    }

    private static void VerifyOptionalHairPatch(string modRoot)
    {
        foreach (string[] config in new[] {
            new[] { "CangYueCao", "RK_sidePonyA", "momanacha.rkhairexpanded", "RK_SideponyO" },
            new[] { "ShanHeYe", "RK_LongA", "rpowder.ratkinhairplus", "RK_Long_I" } })
            VerifyOptionalHairPatch(modRoot, config);
    }

    private static void VerifyOptionalHairPatch(string modRoot, string[] config)
    {
        XmlDocument baseline = new XmlDocument();
        baseline.Load(Path.Combine(modRoot, "Defs/SpecialCharacter/" + config[0] + ".xml"));
        string kindPath = "/Defs/PawnKindDef[defName='TRK_Special_" + config[0] + "']/forcedHair";
        string profilePath = "/Defs/XIYUNTE.SpecialCharacter.SpecialCharacterDef[defName='TRK_Character_" + config[0] + "']/hair";
        Check(baseline.SelectSingleNode(kindPath).InnerText == config[1] &&
            baseline.SelectSingleNode(profilePath).InnerText == config[1], "无扩展发型时使用原鼠族发型：" + config[0]);
        XmlDocument patch = new XmlDocument();
        patch.Load(Path.Combine(modRoot, "Patches/SpecialCharacter/" + config[0] + ".xml"));
        foreach (XmlElement node in patch.SelectNodes("/Patch/Operation/operations/li"))
        {
            Check(node.GetAttribute("MayRequire") == config[2], "发型替换使用指定模组 MayRequire 条件：" + config[0]);
            PatchOperation operation = DirectXmlToObject.ObjectFromXml<PatchOperation>(node, false);
            Check(operation.Apply(baseline), "原版 XML 替换成功：" + node["xpath"].InnerText);
        }
        Check(baseline.SelectSingleNode(kindPath).InnerText == config[3] &&
            baseline.SelectSingleNode(profilePath).InnerText == config[3], "加载扩展发型时模板与人物配置同时替换：" + config[0]);
        Check(baseline.SelectNodes(kindPath).Count == 1 && baseline.SelectNodes(profilePath).Count == 1,
            "发型替换不产生重复字段");
    }

    private static void AddVerificationDef(Type type, Def def)
    {
        typeof(DefDatabase<>).MakeGenericType(type).GetMethod("Add", new[] { type }).Invoke(null, new object[] { def });
    }

    private static List<MemberInfo> ReadILMembers(MethodInfo method)
    {
        Dictionary<short, System.Reflection.Emit.OpCode> opcodes = typeof(System.Reflection.Emit.OpCodes).GetFields()
            .Where(f => f.FieldType == typeof(System.Reflection.Emit.OpCode))
            .Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)).ToDictionary(op => op.Value);
        byte[] body = method.GetMethodBody().GetILAsByteArray();
        List<MemberInfo> members = new List<MemberInfo>();
        for (int offset = 0; offset < body.Length; )
        {
            byte prefix = body[offset++];
            short value = prefix == 0xFE ? (short)(0xFE00 | body[offset++]) : prefix;
            System.Reflection.Emit.OperandType operand = opcodes[value].OperandType;
            if (operand == System.Reflection.Emit.OperandType.InlineMethod || operand == System.Reflection.Emit.OperandType.InlineField)
                members.Add(method.Module.ResolveMember(BitConverter.ToInt32(body, offset)));
            switch (operand)
            {
                case System.Reflection.Emit.OperandType.InlineNone: break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                case System.Reflection.Emit.OperandType.ShortInlineI:
                case System.Reflection.Emit.OperandType.ShortInlineVar: offset += 1; break;
                case System.Reflection.Emit.OperandType.InlineVar: offset += 2; break;
                case System.Reflection.Emit.OperandType.InlineI8:
                case System.Reflection.Emit.OperandType.InlineR: offset += 8; break;
                case System.Reflection.Emit.OperandType.InlineSwitch:
                    offset += 4 + BitConverter.ToInt32(body, offset) * 4; break;
                default: offset += 4; break;
            }
        }
        return members;
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
