using MediatR;

namespace StarCraftKeyManager.Events;

public record ProcessStarted(int ProcessId, string ProcessName) : INotification;