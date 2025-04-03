using MediatR;

namespace KeyRepeatTuner.Events;

public record ProcessStarted(int ProcessId, string ProcessName) : INotification;