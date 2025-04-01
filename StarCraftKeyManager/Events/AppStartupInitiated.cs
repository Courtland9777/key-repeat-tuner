using MediatR;

namespace StarCraftKeyManager.Events;

public record AppStartupInitiated : INotification;