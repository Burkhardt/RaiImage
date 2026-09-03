using System.Runtime.CompilerServices;

namespace RaiImage.Tests;

public class WordCaseCompatibilityTests
{
	[Fact]
	public void RaiImageWordCase_DelegatesToRaiUtilsImplementation()
	{
#pragma warning disable CS0618
		var legacy = new RaiImage.WordCase("nomsa-concert-167");
#pragma warning restore CS0618

		Assert.IsAssignableFrom<RaiUtils.WordCase>(legacy);
		Assert.Equal("NomsaConcert167", legacy.PascalCase);
		Assert.Equal("nomsa_concert_167", legacy.SnakeCase);
	}

	[Fact]
	public void RaiImageStringHelper_PreservesStaticBinarySignaturesWithoutExtensionCollisions()
	{
#pragma warning disable CS0618
		Assert.Equal("Nomsa", RaiImage.StringHelper.ToTitle("nomsa"));
		Assert.Equal(["nomsa", "Concert", "11"], RaiImage.StringHelper.WordSplit("nomsa-Concert_11"));
		Assert.Equal(["nomsa", "Concert", "11"], RaiImage.StringHelper.CamelSplit("nomsa-Concert_11"));
		var legacyMethods = typeof(RaiImage.StringHelper).GetMethods();
#pragma warning restore CS0618

		Assert.DoesNotContain(legacyMethods, method => method.IsDefined(typeof(ExtensionAttribute), inherit: false));
	}

	[Fact]
	public void RaiUtilsExtensions_AreAvailableToRaiImageConsumers()
	{
		Assert.Equal([8, 18], RaiUtils.StringHelper.WordSeams("ScheduleRehearsal_Nomsa"));
		Assert.Equal(["Schedule", "Rehearsal", "Nomsa"], RaiUtils.StringHelper.WordSplit("ScheduleRehearsal_Nomsa"));
	}
}
