using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Content.Application;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.Tests.ContentTypes;

public sealed class AddContentFieldHandlerTests
{
    [Fact]
    public async Task Handle_should_add_field_and_save_changes()
    {
        var contentType = ContentType.Create("product", "Product");
        var repository = new FakeContentTypeRepository(contentType);
        var unitOfWork = new FakeContentUnitOfWork();
        var handler = new AddContentFieldHandler(
            repository,
            unitOfWork,
            new AllowAllContentAccessService(),
            new NullAuditLog(),
            NullLogger<AddContentFieldHandler>.Instance);

        await handler.Handle(
            new AddContentFieldCommand(
                contentType.Id,
                "title",
                "Title",
                FieldKind.String,
                LocalizationMode.Localized,
                IsRequired: true),
            CancellationToken.None);

        contentType.Fields.Should().ContainSingle(field =>
            field.ApiName.Value == "title"
            && field.IsLocalized
            && field.IsRequired);
        repository.UpdatedContentType.Should().BeSameAs(contentType);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_throw_when_content_type_does_not_exist()
    {
        var repository = new FakeContentTypeRepository();
        var handler = new AddContentFieldHandler(
            repository,
            new FakeContentUnitOfWork(),
            new AllowAllContentAccessService(),
            new NullAuditLog(),
            NullLogger<AddContentFieldHandler>.Instance);

        var act = async () => await handler.Handle(
            new AddContentFieldCommand(
                ContentTypeId.New(),
                "title",
                "Title",
                FieldKind.String,
                LocalizationMode.Localized,
                IsRequired: true),
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
}
