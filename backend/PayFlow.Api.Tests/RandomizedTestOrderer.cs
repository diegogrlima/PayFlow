using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestCaseOrderer("PayFlow.Api.Tests.RandomizedTestOrderer", "PayFlow.Api.Tests")]

namespace PayFlow.Api.Tests;

// A fixed default keeps CI reproducible; TEST_ORDER_SEED permits deliberate order variation.
public sealed class RandomizedTestOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var ordered = testCases.OrderBy(test => test.DisplayName, StringComparer.Ordinal).ToArray();
        var seed = int.TryParse(Environment.GetEnvironmentVariable("TEST_ORDER_SEED"), out var configured)
            ? configured : 0;
        var random = new Random(seed);
        for (var index = ordered.Length - 1; index > 0; index--)
        {
            var other = random.Next(index + 1);
            (ordered[index], ordered[other]) = (ordered[other], ordered[index]);
        }
        return ordered;
    }
}
