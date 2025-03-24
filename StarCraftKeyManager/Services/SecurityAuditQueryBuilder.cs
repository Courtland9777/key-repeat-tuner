using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

public class SecurityAuditQueryBuilder : IEventLogQueryBuilder
{
    public EventLogQuery BuildQuery()
    {
        const string query =
            "*[System[Provider[@Name='Microsoft-Windows-Security-Auditing'] and (EventID=4688 or EventID=4689)]]";
        return new EventLogQuery("Security", PathType.LogName, query)
        {
            ReverseDirection = true
        };
    }
}