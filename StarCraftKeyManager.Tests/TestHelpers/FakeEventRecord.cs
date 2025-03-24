using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using System.Security.Principal;

namespace StarCraftKeyManager.Tests.TestHelpers;

public class FakeEventRecord : EventRecord
{
    private readonly List<EventProperty> _properties;

    public FakeEventRecord(int eventId, string processName)
    {
        Id = eventId;

        var eventPropertyType = typeof(EventProperty);
        var ctor = eventPropertyType.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(object)],
            null
        ) ?? throw new InvalidOperationException("Cannot find EventProperty constructor");

        _properties =
        [
            (EventProperty)ctor.Invoke([null]),
            (EventProperty)ctor.Invoke([1234]),
            (EventProperty)ctor.Invoke([processName])
        ];
    }

    public override int Id { get; }
    public override IList<EventProperty> Properties => _properties;

    public override EventBookmark? Bookmark => null;
    public override string? LogName => null;
    public override string? ProviderName => null;
    public override int? Qualifiers => null;
    public override long? RecordId => null;
    public override Guid? RelatedActivityId => null;
    public override DateTime? TimeCreated => null;
    public override int? ThreadId => null;
    public override int? ProcessId => null;
    public override string? MachineName => null;

    public override Guid? ActivityId => null;
    public override byte? Level => null;
    public override short? Opcode => null;
    public override long? Keywords => null;
    public override IEnumerable<string>? KeywordsDisplayNames => null;
    public override int? Task => null;
    public override string? TaskDisplayName => null;
    public override string? OpcodeDisplayName => null;
    public override string? LevelDisplayName => null;
    public override Guid? ProviderId => null;
    public override byte? Version => null;
    public override SecurityIdentifier? UserId => null;

    protected override void Dispose(bool disposing)
    {
    }

    public override string ToXml()
    {
        throw new NotImplementedException();
    }

    public override string? FormatDescription()
    {
        return null;
    }

    public override string FormatDescription(IEnumerable<object> values)
    {
        throw new NotImplementedException();
    }
}