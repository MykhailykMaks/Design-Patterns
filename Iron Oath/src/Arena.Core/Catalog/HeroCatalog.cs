namespace Arena.Core;

public static class HeroCatalog
{
    public static SkillDefinition BasicAttack { get; } = Skill(
        "attack", "Атака", "Завдає шкоди в розмірі 100% атаки одному ворогу.",
        TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1));

    public static IReadOnlyList<HeroDefinition> PlayerHeroes { get; } = Array.AsReadOnly(new[]
    {
        new HeroDefinition("daran", "Даран", HeroRole.Damage, 110, 22,
            Skill("decisive", "Вирішальний удар", "Б’є одного ворога на 180% атаки. Якщо ворога отруєно, сила удару зростає до 225%.", TargetKind.Enemy,
                new SkillEffect(EffectKind.Damage, 1.8, PoisonedTargetBonus: .25)), AuraKind.Attack, CritChance: 20),
        new HeroDefinition("lira", "Руна", HeroRole.Damage, 85, 24,
            Skill("arrows", "Злива стріл", "Випускає стріли в усіх ворогів. Кожна ціль отримує шкоду в розмірі 75% атаки Руни.", TargetKind.AllEnemies,
                new SkillEffect(EffectKind.Damage, .75)), CritChance: 25,
            BasicSkillOverride: Skill("staggering-shot", "Прицільний постріл",
                "Завдає одному ворогу шкоди в розмірі 100% атаки. Шанс оглушити ціль і забрати її наступну дію — 10%.",
                TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1), new SkillEffect(EffectKind.Stun, ChancePercent: 10))),
        new HeroDefinition("mira", "Естель", HeroRole.Support, 95, 16,
            Skill("light", "Живе світло", "Знімає отруту й відновлює здоров’я одному союзнику або собі. Сила лікування дорівнює подвоєній атаці Естель.", TargetKind.Ally,
                new SkillEffect(EffectKind.Heal, 2), new SkillEffect(EffectKind.CleansePoison)), CritChance: 10),
        new HeroDefinition("saira", "Веспер", HeroRole.Control, 90, 19,
            Skill("fade", "Згасання", "80% атаки та отрута на два раунди. Отрута зменшує шкоду ударів цілі на 20% і завдає 6 шкоди наприкінці раунду.", TargetKind.Enemy,
                new SkillEffect(EffectKind.Damage, .8), new SkillEffect(EffectKind.Poison)), CritChance: 15),
        new HeroDefinition("toren", "Торен", HeroRole.Defender, 140, 14,
            Skill("cover", "Прикриття", "Захищає одного союзника. Запас щита дорівнює подвоєній атаці Торена. Якщо щит уже є, залишається міцніший.", TargetKind.Ally,
                new SkillEffect(EffectKind.Shield, 2)), AuraKind.Health, CritChance: 10)
    });

    public static IReadOnlyList<HeroDefinition> EnemyHeroes { get; } = Array.AsReadOnly(new[]
    {
        new HeroDefinition("crusher", "Крушник", HeroRole.Damage, 115, 20,
            Skill("fracture", "Розлом",
                "Завдає одному ворогу шкоди в розмірі 180% атаки. Шанс оглушити ціль і забрати її наступну дію — 25%.", TargetKind.Enemy,
                new SkillEffect(EffectKind.Damage, 1.8), new SkillEffect(EffectKind.Stun, ChancePercent: 25)), AuraKind.Attack, CritChance: 15),
        new HeroDefinition("ash-archer", "Попелястий стрілець", HeroRole.Damage, 80, 22,
            Skill("bolts", "Віяло болтів", "70% атаки всім живим ворогам.", TargetKind.AllEnemies,
                new SkillEffect(EffectKind.Damage, .7)), CritChance: 20),
        new HeroDefinition("reaper", "Жнець", HeroRole.Damage, 90, 20,
            Skill("harvest", "Жнива", "120% атаки одному ворогу та лікування себе на 60% атаки.", TargetKind.Enemy,
                new SkillEffect(EffectKind.Damage, 1.2), new SkillEffect(EffectKind.HealSelf, .6)), CritChance: 15),
        new HeroDefinition("bone-doctor", "Кістяний лікар", HeroRole.Support, 90, 15,
            Skill("stitch", "Зшивання", "Відновлює союзнику здоров’я в розмірі подвоєної атаки лікаря.", TargetKind.Ally,
                new SkillEffect(EffectKind.Heal, 2)), AuraKind.Health, CritChance: 5),
        new HeroDefinition("seal-keeper", "Хранитель печаті", HeroRole.Support, 115, 14,
            Skill("seal", "Печатка", "Дає союзнику щит із запасом, що дорівнює подвоєній атаці Хранителя.", TargetKind.Ally,
                new SkillEffect(EffectKind.Shield, 2)), CritChance: 5),
        new HeroDefinition("hexer", "Гексер", HeroRole.Control, 85, 18,
            Skill("exhaust", "Виснаження",
                "Завдає одному ворогу шкоди в розмірі 70% атаки та послаблює його наступну дію на 25%. Шанс оглушити ціль і забрати її наступну дію — 25%.", TargetKind.Enemy,
                new SkillEffect(EffectKind.Damage, .7), new SkillEffect(EffectKind.Weaken), new SkillEffect(EffectKind.Stun, ChancePercent: 25)), CritChance: 10),
        new HeroDefinition("will-weaver", "Ткач волі", HeroRole.Control, 95, 16,
            Skill("order", "Наказ", "Союзник завдасть на 25% більше шкоди під час наступної дії. Лікування або щит також витратить ефект.", TargetKind.Ally,
                new SkillEffect(EffectKind.Empower)), AuraKind.Attack, CritChance: 10)
    });

    public static HeroDefinition Get(string id) => PlayerHeroes.Concat(EnemyHeroes)
        .FirstOrDefault(hero => hero.Id == id)
        ?? throw new ArgumentException("Невідомий герой.", nameof(id));

    private static SkillDefinition Skill(string id, string name, string description,
        TargetKind target, params SkillEffect[] effects) =>
        new(id, name, description, target, Array.AsReadOnly(effects));
}
