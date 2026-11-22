namespace Arena.Core;

public sealed record HeroDefinition(
    string Id, string Name, HeroRole Role, int MaxHealth, int Attack,
    SkillDefinition SpecialSkill, AuraKind Aura = AuraKind.None, int CritChance = 0, SkillDefinition? BasicSkillOverride = null)
{
    public SkillDefinition BasicSkill => BasicSkillOverride ?? HeroCatalog.BasicAttack;
}
