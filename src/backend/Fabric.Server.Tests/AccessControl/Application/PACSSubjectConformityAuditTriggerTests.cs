using Fabric.Server.AccessControl.Application;

namespace Fabric.Server.Tests.AccessControl.Application;

public sealed class PACSSubjectConformityAuditTriggerTests
{
    [Fact]
    public async Task EnqueueAsync_WhenSameWorkItemAlreadyQueued_Deduplicates()
    {
        PACSSubjectConformityAuditTrigger trigger = new();
        PACSSubjectConformityAuditWorkItem workItem = new("tenant", Guid.NewGuid(), Guid.NewGuid());

        await trigger.EnqueueAsync(workItem);
        await trigger.EnqueueAsync(workItem);

        Assert.True(trigger.TryRead(out PACSSubjectConformityAuditWorkItem? first));
        Assert.NotNull(first);
        Assert.False(trigger.TryRead(out _));
    }

    [Fact]
    public async Task EnqueueAsync_AfterComplete_AllowsSameWorkItemAgain()
    {
        PACSSubjectConformityAuditTrigger trigger = new();
        PACSSubjectConformityAuditWorkItem workItem = new("tenant", Guid.NewGuid(), Guid.NewGuid());

        await trigger.EnqueueAsync(workItem);
        Assert.True(trigger.TryRead(out PACSSubjectConformityAuditWorkItem? first));
        Assert.NotNull(first);
        trigger.Complete(first!);

        await trigger.EnqueueAsync(workItem);

        Assert.True(trigger.TryRead(out PACSSubjectConformityAuditWorkItem? second));
        Assert.NotNull(second);
    }
}
