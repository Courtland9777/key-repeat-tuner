using System.Diagnostics.Eventing.Reader;

namespace StarCraftKeyManager.Interfaces;

public interface IEventLogQueryBuilder
{
    EventLogQuery BuildQuery();
}