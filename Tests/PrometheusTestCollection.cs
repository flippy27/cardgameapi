using Xunit;

namespace CardDuel.ServerApi.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PrometheusTestCollection
{
    public const string Name = "Prometheus metrics static state";
}
