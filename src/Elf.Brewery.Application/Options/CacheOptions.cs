using System.ComponentModel.DataAnnotations;

namespace Elf.Brewery.Application.Options;

public sealed class CacheOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "Cache:ExpirationMinutes must be at least 1.")]
    public int ExpirationMinutes { get; set; }

    // Controls how old the persisted data may get before a read triggers an external
    // refresh. Deliberately separate from ExpirationMinutes: the cache TTL is a local
    // performance knob (how long to skip a cheap DB read), while this is a cost/reliability
    // knob (how long to skip an expensive external API call). They can be tuned independently.
    [Range(1, int.MaxValue, ErrorMessage = "Cache:ExternalRefreshMinutes must be at least 1.")]
    public int ExternalRefreshMinutes { get; set; }
}