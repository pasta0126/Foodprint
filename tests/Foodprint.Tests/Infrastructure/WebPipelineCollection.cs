using Xunit;

namespace Foodprint.Tests.Infrastructure;

/// <summary>
/// One shared <see cref="FoodprintWebFactory"/> (and therefore one booted app + one
/// SQLite file) for every full-pipeline HTTP test. Sharing keeps these classes from
/// running in parallel against the same on-disk database.
/// </summary>
[CollectionDefinition(nameof(WebPipelineCollection))]
public sealed class WebPipelineCollection : ICollectionFixture<FoodprintWebFactory>;
