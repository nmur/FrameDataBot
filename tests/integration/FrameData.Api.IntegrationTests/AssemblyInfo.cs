using Xunit;

// WebApplicationFactory boots the full API host, including process-wide logging state.
// Keep this integration assembly serialized so independent fixture datasets and host
// shutdown do not race in CI.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
