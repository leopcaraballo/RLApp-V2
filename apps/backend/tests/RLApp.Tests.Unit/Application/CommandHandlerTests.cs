namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RLApp.Application.Commands;
using RLApp.Application.DTOs;
using RLApp.Application.Handlers;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

public class ActivateConsultingRoomHandlerTests
{
    private readonly IConsultingRoomRepository _roomRepo = Substitute.For<IConsultingRoomRepository>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IPersistenceSession _persistenceSession = Substitute.For<IPersistenceSession>();
    private readonly ActivateConsultingRoomHandler _sut;

    public ActivateConsultingRoomHandlerTests()
    {
        _sut = new ActivateConsultingRoomHandler(_roomRepo, _eventPublisher, _auditStore, _persistenceSession);
    }

    [Fact]
    public async Task Handle_NewRoom_CreatesAndActivatesRoom()
    {
        _roomRepo.GetByIdAsync("room-new", Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var command = new ActivateConsultingRoomCommand("room-new", "Consultorio Nuevo", "corr-act", "user-01");
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        await _roomRepo.Received(1).AddAsync(Arg.Any<ConsultingRoom>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
        await _persistenceSession.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveRoom_ReactivatesRoom()
    {
        var existingRoom = ConsultingRoom.Create("room-inactive", "Consultorio", "corr-old");
        existingRoom.Deactivate("corr-deac");
        existingRoom.ClearUnraisedEvents();

        _roomRepo.GetByIdAsync("room-inactive", Arg.Any<CancellationToken>())
            .Returns(existingRoom);

        var command = new ActivateConsultingRoomCommand("room-inactive", "Consultorio", "corr-act2", "user-01");
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        await _roomRepo.Received(1).UpdateAsync(Arg.Any<ConsultingRoom>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyActiveRoom_ReturnsFailure()
    {
        var activeRoom = ConsultingRoom.Create("room-active", "Consultorio", "corr-old");
        activeRoom.ClearUnraisedEvents();

        _roomRepo.GetByIdAsync("room-active", Arg.Any<CancellationToken>())
            .Returns(activeRoom);

        var command = new ActivateConsultingRoomCommand("room-active", "Consultorio", "corr-act3", "user-01");
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
    }
}

public class CallNextAtCashierHandlerTests
{
    private readonly IWaitingQueueRepository _queueRepo = Substitute.For<IWaitingQueueRepository>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IPersistenceSession _persistenceSession = Substitute.For<IPersistenceSession>();
    private readonly CallNextAtCashierHandler _sut;

    public CallNextAtCashierHandlerTests()
    {
        _sut = new CallNextAtCashierHandler(_queueRepo, _eventPublisher, _auditStore, _persistenceSession);
    }

    [Fact]
    public async Task Handle_QueueWithPatients_CallsNextPatient()
    {
        var queue = WaitingQueue.Create("q-1", "Queue", "corr-old");
        queue.Open();
        queue.CheckInPatient("PAT-001", "Alice", "APP-001", 0, null, "corr-old");
        queue.ClearUnraisedEvents();

        _queueRepo.GetByIdAsync("q-1", Arg.Any<CancellationToken>()).Returns(queue);

        var command = new CallNextAtCashierCommand("q-1", "CAJA-01", "corr-call", "user-01");
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        await _queueRepo.Received(1).UpdateAsync(Arg.Any<WaitingQueue>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyQueue_ReturnsFailure()
    {
        var queue = WaitingQueue.Create("q-empty", "Queue", "corr-old");
        queue.Open();
        queue.ClearUnraisedEvents();

        _queueRepo.GetByIdAsync("q-empty", Arg.Any<CancellationToken>()).Returns(queue);

        var command = new CallNextAtCashierCommand("q-empty", "CAJA-01", "corr-fail", "user-01");
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
    }
}
