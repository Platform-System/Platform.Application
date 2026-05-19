using FluentValidation;
using MediatR;
using Platform.Application.Behaviors;
using Platform.Application.Messaging;
using Platform.BuildingBlocks.Responses;
using Xunit;

namespace Platform.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenAsyncValidatorFails_ReturnsFailureResult()
    {
        var behavior = new ValidationBehavior<SampleCommand, Result<string>>(
            [new SampleCommandValidator()]);

        var response = await behavior.Handle(
            new SampleCommand(string.Empty),
            new RequestHandlerDelegate<Result<string>>(_ => Task.FromResult(Result<string>.Success("ok"))),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal("Name is required.", Assert.Single(response.Errors));
    }

    [Fact]
    public async Task Handle_WhenNoValidators_ReturnsNextResponse()
    {
        var behavior = new ValidationBehavior<SampleCommand, Result<string>>([]);

        var response = await behavior.Handle(
            new SampleCommand("valid"),
            new RequestHandlerDelegate<Result<string>>(_ => Task.FromResult(Result<string>.Success("ok"))),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal("ok", response.Value);
    }

    [Fact]
    public async Task Handle_WhenResponseTypeIsNotResult_ThrowsInvalidOperationException()
    {
        var behavior = new ValidationBehavior<PlainRequest, string>(
            [new PlainRequestValidator()]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new PlainRequest(string.Empty),
                new RequestHandlerDelegate<string>(_ => Task.FromResult("ok")),
                CancellationToken.None));

        Assert.Contains("Result<T>", exception.Message);
    }

    private sealed record SampleCommand(string Name) : ICommand<string>;

    private sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(x => x.Name)
                .MustAsync(async (name, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    return !string.IsNullOrWhiteSpace(name);
                })
                .WithMessage("Name is required.");
        }
    }

    private sealed record PlainRequest(string Name) : IRequest<string>;

    private sealed class PlainRequestValidator : AbstractValidator<PlainRequest>
    {
        public PlainRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
