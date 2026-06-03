using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Application.Abstractions.Data;
using Platform.Application.Behaviors;
using Platform.Application.Messaging;
using Platform.BuildingBlocks.Responses;
using Platform.Domain.Common;
using Xunit;

namespace Platform.Application.Tests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCommandReturnsFailure_DoesNotCommitOrSaveChanges()
    {
        var unitOfWork = new FakeUnitOfWork();
        var mediator = new FakeMediator(unitOfWork);
        var behavior = new TransactionBehavior<FailingCommand, Result<Unit>>(
            unitOfWork,
            mediator,
            NullLogger<TransactionBehavior<FailingCommand, Result<Unit>>>.Instance);

        var response = await behavior.Handle(
            new FailingCommand(),
            _ => Task.FromResult(Result<Unit>.Failure(400, "Business rule failed.")),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0, unitOfWork.Transaction.CommitCallCount);
        Assert.Equal(1, unitOfWork.Transaction.RollbackCallCount);
    }

    [Fact]
    public async Task Handle_WhenCommandHasPreCommitEvents_PublishesBeforeSaveChangesAndCommit()
    {
        var unitOfWork = new FakeUnitOfWork();
        var mediator = new FakeMediator(unitOfWork);
        var behavior = new TransactionBehavior<PreCommitCommand, Result<Unit>>(
            unitOfWork,
            mediator,
            NullLogger<TransactionBehavior<PreCommitCommand, Result<Unit>>>.Instance);

        var command = new PreCommitCommand();
        command.PreCommitEvents.Add(new SampleNotification("pre"));

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(Result<Unit>.Success(Unit.Value)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(["publish:pre|saved:0|committed:0", "save", "commit"], unitOfWork.ExecutionLog);
        Assert.Empty(command.PreCommitEvents);
    }

    [Fact]
    public async Task Handle_WhenCommandHasPostCommitEvents_PublishesAfterSaveChangesAndCommit()
    {
        var unitOfWork = new FakeUnitOfWork();
        var mediator = new FakeMediator(unitOfWork);
        var behavior = new TransactionBehavior<PostCommitCommand, Result<Unit>>(
            unitOfWork,
            mediator,
            NullLogger<TransactionBehavior<PostCommitCommand, Result<Unit>>>.Instance);

        var command = new PostCommitCommand();
        command.PostCommitEvents.Add(new SampleNotification("post"));

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(Result<Unit>.Success(Unit.Value)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(["save", "commit", "publish:post|saved:1|committed:1"], unitOfWork.ExecutionLog);
        Assert.Empty(command.PostCommitEvents);
    }

    private sealed class FailingCommand : ICommand
    {
    }

    private sealed class PreCommitCommand : ICommand, IHasPreCommitEvent
    {
        public List<INotification> PreCommitEvents { get; } = [];
    }

    private sealed class PostCommitCommand : ICommand, IHasPostCommitEvent
    {
        public List<INotification> PostCommitEvents { get; } = [];
    }

    private sealed record SampleNotification(string Name) : INotification;

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork()
        {
            Transaction = new FakeDbContextTransaction(this);
        }

        public FakeDbContextTransaction Transaction { get; }
        public int SaveChangesCallCount { get; private set; }
        public bool HasActiveTransaction { get; private set; }
        public List<string> ExecutionLog { get; } = [];

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            return Task.FromResult<IDbContextTransaction>(Transaction);
        }

        public IGenericRepository<T> GetRepository<T>() where T : Entity
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            ExecutionLog.Add("save");
            return Task.FromResult(1);
        }
    }

    private sealed class FakeDbContextTransaction : IDbContextTransaction
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakeDbContextTransaction(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Guid TransactionId { get; } = Guid.NewGuid();
        public int CommitCallCount { get; private set; }
        public int RollbackCallCount { get; private set; }
        public bool SupportsSavepoints => false;

        public void Commit()
        {
            CommitCallCount++;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCallCount++;
            _unitOfWork.ExecutionLog.Add("commit");
            return Task.CompletedTask;
        }

        public void CreateSavepoint(string name)
        {
        }

        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void ReleaseSavepoint(string name)
        {
        }

        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Rollback()
        {
            RollbackCallCount++;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCallCount++;
            return Task.CompletedTask;
        }

        public void RollbackToSavepoint(string name)
        {
        }

        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMediator : IMediator
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakeMediator(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification is SampleNotification sample)
            {
                _unitOfWork.ExecutionLog.Add(
                    $"publish:{sample.Name}|saved:{_unitOfWork.SaveChangesCallCount}|committed:{_unitOfWork.Transaction.CommitCallCount}");
            }

            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
