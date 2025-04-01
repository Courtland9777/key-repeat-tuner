using MediatR;

namespace StarCraftKeyManager.Events;

public record ProcessStopped(int ProcessId, string ProcessName) : INotification;