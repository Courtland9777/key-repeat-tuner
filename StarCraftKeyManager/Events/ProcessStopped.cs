using MediatR;

namespace KeyRepeatTuner.Events;

public record ProcessStopped(int ProcessId, string ProcessName) : INotification;