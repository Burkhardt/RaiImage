namespace RaiImage.Tests;

public class WordCaseTests
{
	private const string PascalName = "NomsaConcert167";
	private const string CamelName = "nomsaConcert167";
	private const string SnakeName = "nomsa_concert_167";
	private const string KebabName = "nomsa-concert-167";

	[Theory]
	[InlineData("San-Diego-State-09.24-212", "SanDiegoState0924212", "sanDiegoState0924212", "san_diego_state_09_24_212", "san-diego-state-09-24-212")]
	[InlineData("nomsa-concert-167", PascalName, CamelName, SnakeName, KebabName)]
	[InlineData("Mixed_Snake.AndPascal-and-kebabCase", "MixedSnakeAndPascalAndKebabCase", "mixedSnakeAndPascalAndKebabCase", "mixed_snake_and_pascal_and_kebab_case", "mixed-snake-and-pascal-and-kebab-case")]
	public void StringConstructor_DetectsInputCaseAndConvertsToAllOutputCases(
		string name,
		string expectedPascalCase,
		string expectedLowerCamelCase,
		string expectedSnakeCase,
		string expectedKebabCase)
	{
		var sut = new WordCase(name);

		Assert.Equal(expectedPascalCase, sut.PascalCase);
		Assert.Equal(expectedLowerCamelCase, sut.LowerCamelCase);
		Assert.Equal(expectedSnakeCase, sut.SnakeCase);
		Assert.Equal(expectedKebabCase, sut.KebabCase);
	}

	[Fact]
	public void ArrayConstructor_ConvertsWordsToAllOutputCases()
	{
		var sut = new WordCase(["nomsa", "concert", "167"]);

		Assert.Equal(PascalName, sut.PascalCase);
		Assert.Equal(CamelName, sut.LowerCamelCase);
		Assert.Equal(SnakeName, sut.SnakeCase);
		Assert.Equal(KebabName, sut.KebabCase);
	}

	[Fact]
	public void CamelCaseString_RemainsCompatibilityAliasForLowerCamelCase()
	{
		var sut = new WordCase(SnakeName);

		Assert.Equal(CamelName, sut.CamelCaseString);
		Assert.Equal(sut.LowerCamelCase, sut.CamelCaseString);
	}

	[Fact]
	public void DashCase_RemainsAliasForKebabCase()
	{
		var sut = new WordCase(SnakeName);

		Assert.Equal(KebabName, sut.DashCase);
		Assert.Equal(sut.KebabCase, sut.DashCase);
	}

	[Fact]
	public void WordSplit_SplitsMixedCaseAndSeparatorInputIntoWords()
	{
		var words = "nomsa-Concert_11".WordSplit();

		Assert.Equal(["nomsa", "Concert", "11"], words);
	}

	[Fact]
	public void CamelSplit_RemainsCompatibilityAliasForWordSplit()
	{
		var name = "nomsa-Concert_11";

		Assert.Equal(name.WordSplit(), name.CamelSplit());
	}

	[Fact]
	public void StringProperty_DefaultsToPascalCaseForLegacyCallers()
	{
		var sut = new WordCase(SnakeName);

		Assert.Equal(PascalName, sut.String);
	}
}