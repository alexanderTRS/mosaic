using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Content.Application;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.Tests.ContentTypes;

public sealed class PublishContentTypeHandlerTests
{
    [Fact]
    public async Task Handle_should_publish_content_type_and_save_changes()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true));
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeContentTypeRepository(contentType);
        var unitOfWork = new FakeContentUnitOfWork();
        var handler = new PublishContentTypeHandler(
            repository,
            unitOfWork,
            clock,
            new AllowAllContentAccessService(),
            new NullAuditLog(),
            new NoOpSchemaChangeNotifier(),
            NullLogger<PublishContentTypeHandler>.Instance);

        var result = await handler.Handle(
            new PublishContentTypeCommand(contentType.Id),
            CancellationToken.None);

        result.Status.Should().Be(ContentTypeStatus.Published);
        result.PublishedAt.Should().Be(clock.UtcNow);
        result.Fields.Should().ContainSingle(field => field.ApiName == "title");
        repository.UpdatedContentType.Should().BeSameAs(contentType);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_throw_when_content_type_does_not_exist()
    {
        var handler = new PublishContentTypeHandler(
            new FakeContentTypeRepository(),
            new FakeContentUnitOfWork(),
            new FixedClock(DateTimeOffset.UtcNow),
            new AllowAllContentAccessService(),
            new NullAuditLog(),
            new NoOpSchemaChangeNotifier(),
            NullLogger<PublishContentTypeHandler>.Instance);

        var act = async () => await handler.Handle(
            new PublishContentTypeCommand(ContentTypeId.New()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ContentTypeNotFoundException>();
    }

    private sealed class FakeContentTypeRepository : IContentTypeRepository
    {
        private readonly ContentType? contentType;

        public FakeContentTypeRepository(ContentType? contentType = null)
        {
            this.contentType = contentType;
        }

        public ContentType? UpdatedContentType { get; private set; }

        public Task<ContentType?> GetById(ContentTypeId contentTypeId, CancellationToken cancellationToken)
            => Task.FromResult(contentType?.Id == contentTypeId ? contentType : null);

        public Task<ContentType?> GetByApiName(string apiName, CancellationToken cancellationToken)
            => Task.FromResult(contentType?.ApiName.Value == apiName ? contentType : null);

        public Task<IReadOnlyCollection<ContentType>> List(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ContentType>>(
                contentType is null ? [] : [contentType]);

        public Task Add(ContentType contentType, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task Update(ContentType contentType, CancellationToken cancellationToken)
        {
            UpdatedContentType = contentType;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContentUnitOfWork : IContentUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task SaveChanges(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class NoOpSchemaChangeNotifier : IContentSchemaChangeNotifier
    {
        public Task PublishedContentTypesChanged(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
