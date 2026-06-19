using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tarot.SpreadReading
{
    public static class SpreadDefinitionCatalog
    {
        private static readonly SpreadDefinition[] definitions =
        {
            new(
                "free-three-card",
                "无牌阵三张牌",
                "适合 万能通用问题的快速抽牌",
                "不限定过去、现在或未来，用三张牌给问题提供开放式线索。",
                SpreadRevealFlow.ImmediateReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("线索一", "第一条关键线索", new Vector2(0.3f, 0.36f), new Vector2(-150f, -156f)),
                    new SpreadCardSlotDefinition("线索二", "第二条关键线索", new Vector2(0.5f, 0.36f), new Vector2(0f, -156f)),
                    new SpreadCardSlotDefinition("线索三", "第三条关键线索", new Vector2(0.7f, 0.36f), new Vector2(150f, -156f))
                }),
            new(
                "time-flow",
                "时间牌阵",
                "适合 过去、现在、未来的脉络梳理",
                "用三张牌看清事情从哪里来、正在如何展开、接下来会走向哪里。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("过去", "事件的过去情况", new Vector2(0.32f, 0.42f), new Vector2(-150f, -156f)),
                    new SpreadCardSlotDefinition("现在", "事件的现在状态", new Vector2(0.5f, 0.42f), new Vector2(0f, -156f)),
                    new SpreadCardSlotDefinition("未来", "事件的未来发展", new Vector2(0.68f, 0.42f), new Vector2(150f, -156f))
                }),
            new(
                "holy-triangle",
                "圣三角",
                "适合 快速看清原因、现况和结果",
                "用三张牌聚焦问题的起因、正在发生的状态，以及最可能呈现的结果。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("原因", "问题的起因", new Vector2(0.38f, 0.3f), new Vector2(-86f, -170f)),
                    new SpreadCardSlotDefinition("现况", "问题的现在状态", new Vector2(0.62f, 0.3f), new Vector2(86f, -170f)),
                    new SpreadCardSlotDefinition("结果", "事情的后续走向", new Vector2(0.5f, 0.72f), new Vector2(0f, -50f))
                }),
            new(
                "lover-pyramid",
                "恋人金字塔",
                "适合 关系期待、对方感受和未来关系",
                "用四张牌观察你、对方、目前关系，以及关系可能发展的方向。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("自己", "你对这段关系的期待", new Vector2(0.5f, 0.2f), new Vector2(0f, -188f)),
                    new SpreadCardSlotDefinition("对方", "对方对你的看法", new Vector2(0.34f, 0.5f), new Vector2(-78f, -104f)),
                    new SpreadCardSlotDefinition("目前关系", "你认为目前的关系", new Vector2(0.66f, 0.5f), new Vector2(78f, -104f)),
                    new SpreadCardSlotDefinition("未来关系", "你期望双方关系的发展结果", new Vector2(0.5f, 0.8f), new Vector2(0f, -20f))
                }),
            new(
                "love-cross",
                "爱情十字",
                "适合 自己想法、对方想法和关系结果",
                "用十字结构看见自己、对方、阻碍、外部环境与关系发展结果。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("自己", "你自己的想法", new Vector2(0.5f, 0.5f), new Vector2(0f, -142f)),
                    new SpreadCardSlotDefinition("对方", "对方的想法", new Vector2(0.5f, 0.78f), new Vector2(0f, -54f)),
                    new SpreadCardSlotDefinition("阻碍", "当前问题中的阻碍", new Vector2(0.3f, 0.62f), new Vector2(-118f, -94f)),
                    new SpreadCardSlotDefinition("外部环境", "二人目前的人文环境", new Vector2(0.7f, 0.62f), new Vector2(118f, -94f)),
                    new SpreadCardSlotDefinition("结果", "双方关系发展的结果", new Vector2(0.5f, 0.22f), new Vector2(0f, 34f))
                }),
            new(
                "choice",
                "二选一",
                "适合 两个选择之间的取舍",
                "用五张牌比较现况、两个选择的未来，以及各自可能呈现的结果。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("现况", "当前事情的现况", new Vector2(0.5f, 0.34f), new Vector2(0f, -126f)),
                    new SpreadCardSlotDefinition("选择 A", "选择 A 的近期未来", new Vector2(0.32f, 0.49f), new Vector2(-128f, -72f)),
                    new SpreadCardSlotDefinition("选择 B", "选择 B 的近期未来", new Vector2(0.68f, 0.49f), new Vector2(128f, -72f)),
                    new SpreadCardSlotDefinition("A 结果", "选择 A 的结果", new Vector2(0.24f, 0.78f), new Vector2(-168f, 24f)),
                    new SpreadCardSlotDefinition("B 结果", "选择 B 的结果", new Vector2(0.76f, 0.78f), new Vector2(168f, 24f))
                }),
            new(
                "relationship-development",
                "关系发展",
                "适合 关系如何推进、阻碍和结果",
                "用六张牌观察你、对方、当前关系、发展障碍、期待和结果。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("你", "你对对方的看法", new Vector2(0.34f, 0.22f), new Vector2(-192f, -178f)),
                    new SpreadCardSlotDefinition("对方", "对方对你的看法", new Vector2(0.66f, 0.22f), new Vector2(-64f, -178f)),
                    new SpreadCardSlotDefinition("目前关系", "你认为目前的关系", new Vector2(0.34f, 0.5f), new Vector2(-192f, -90f)),
                    new SpreadCardSlotDefinition("对方认知", "对方认为目前的关系", new Vector2(0.66f, 0.5f), new Vector2(-64f, -90f)),
                    new SpreadCardSlotDefinition("期待", "你期望关系的发展结果", new Vector2(0.34f, 0.78f), new Vector2(-192f, -2f)),
                    new SpreadCardSlotDefinition("结果", "对方期望关系的发展结果", new Vector2(0.66f, 0.78f), new Vector2(-64f, -2f))
                }),
            new(
                "hexagram",
                "问题六芒星",
                "适合 复杂问题的过去、现况、未来和指引",
                "用七张牌从过去、现况、未来、指引、环境、期望和结果拆解问题。",
                SpreadRevealFlow.StagedReveal,
                new[]
                {
                    new SpreadCardSlotDefinition("过去", "问题的过去", new Vector2(0.28f, 0.5f), new Vector2(-120f, -122f)),
                    new SpreadCardSlotDefinition("现况", "问题的现况", new Vector2(0.4f, 0.22f), new Vector2(-60f, -200f)),
                    new SpreadCardSlotDefinition("未来", "问题的未来", new Vector2(0.6f, 0.22f), new Vector2(60f, -200f)),
                    new SpreadCardSlotDefinition("指引", "问题的指引", new Vector2(0.72f, 0.5f), new Vector2(120f, -122f)),
                    new SpreadCardSlotDefinition("环境", "问题的环境", new Vector2(0.6f, 0.78f), new Vector2(60f, -44f)),
                    new SpreadCardSlotDefinition("期望", "内心的期望", new Vector2(0.4f, 0.78f), new Vector2(-60f, -44f)),
                    new SpreadCardSlotDefinition("结果", "问题的结果", new Vector2(0.5f, 0.5f), new Vector2(0f, -122f))
                })
        };

        public static IReadOnlyList<SpreadDefinition> All => definitions;

        public static SpreadDefinition GetDefault()
        {
            return definitions[0];
        }
    }

    public sealed class SpreadDefinition
    {
        public SpreadDefinition(
            string id,
            string title,
            string summary,
            string description,
            SpreadRevealFlow revealFlow,
            SpreadCardSlotDefinition[] slots)
        {
            Id = id;
            Title = title;
            Summary = summary;
            Description = description;
            RevealFlow = revealFlow;
            Slots = slots ?? Array.Empty<SpreadCardSlotDefinition>();
        }

        public string Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public string Description { get; }
        public SpreadRevealFlow RevealFlow { get; }
        public IReadOnlyList<SpreadCardSlotDefinition> Slots { get; }
        public int CardCount => Slots.Count;
    }

    public enum SpreadRevealFlow
    {
        ImmediateReveal,
        StagedReveal
    }

    public readonly struct SpreadCardSlotDefinition
    {
        public SpreadCardSlotDefinition(string name, string meaning, Vector2 viewportPosition, Vector2 previewPosition)
        {
            Name = name;
            Meaning = meaning;
            ViewportPosition = viewportPosition;
            PreviewPosition = previewPosition;
        }

        public string Name { get; }
        public string Meaning { get; }
        public Vector2 ViewportPosition { get; }
        public Vector2 PreviewPosition { get; }
    }
}
