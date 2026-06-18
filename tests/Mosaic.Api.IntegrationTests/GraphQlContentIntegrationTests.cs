using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Content.GraphQL.Dynamic;

namespace Mosaic.Api.IntegrationTests;

public sealed class GraphQlContentIntegrationTests : IClassFixture<MosaicApiFactory>
{
    private readonly MosaicApiFactory factory;

    public GraphQlContentIntegrationTests(MosaicApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_publish_and_query_content_item_should_work_against_postgresql()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var contentTypeId = await CreateContentType("productIntegration", "Product Integration");

        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) {
                contentTypeId
              }
            }
            """,
            new { contentTypeId });

        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) {
                id
                apiName
                status
              }
            }
            """,
            new { contentTypeId });

        var createdItem = await PostGraphQl(
            """
            mutation CreateItem($data: String!) {
              createContentItem(input: {
                contentTypeApiName: "productIntegration",
                data: $data
              }) {
                id
                contentTypeApiName
                status
                data
              }
            }
            """,
            new { data = """{"title":"Demo product"}""" });

        var contentItemId = createdItem.RootElement
            .GetProperty("data")
            .GetProperty("createContentItem")
            .GetProperty("id")
            .GetString();

        contentItemId.Should().NotBeNullOrWhiteSpace();

        var queriedItem = await PostGraphQl(
            """
            query Item($id: UUID!) {
              contentItem(id: $id) {
                id
                contentTypeApiName
                status
                data
              }
            }
            """,
            new { id = contentItemId });

        var item = queriedItem.RootElement.GetProperty("data").GetProperty("contentItem");
        item.GetProperty("id").GetString().Should().Be(contentItemId);
        item.GetProperty("contentTypeApiName").GetString().Should().Be("productIntegration");
        item.GetProperty("status").GetString().Should().Be("DRAFT");
        using var data = JsonDocument.Parse(item.GetProperty("data").GetString()!);
        data.RootElement.GetProperty("title").GetString().Should().Be("Demo product");
    }

    [Fact]
    public async Task Protected_content_mutation_should_return_access_denied_without_authentication()
    {
        factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var response = await factory.HttpClient.PostAsJsonAsync(
            "/graphql",
            new GraphQlRequest(
                """
                mutation {
                  createContentType(input: { apiName: "deniedIntegration", displayName: "Denied" }) {
                    id
                  }
                }
                """));

        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        document.RootElement
            .GetProperty("errors")[0]
            .GetProperty("extensions")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task Media_asset_upload_query_and_download_should_work_against_postgresql()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("demo media"));
        var uploaded = await PostGraphQl(
            """
            mutation Upload($base64Content: String!) {
              uploadMediaAsset(input: {
                fileName: "product.txt",
                contentType: "text/plain",
                base64Content: $base64Content,
                altText: "Product file",
                localizedAltText: [{ locale: "ru-ru", altText: "Файл товара" }]
              }) {
                id
                fileName
                contentType
                size
                publicUrl
                altText
                localizedAltText { locale altText }
              }
            }
            """,
            new { base64Content });

        var asset = uploaded.RootElement.GetProperty("data").GetProperty("uploadMediaAsset");
        var assetId = asset.GetProperty("id").GetString();
        asset.GetProperty("fileName").GetString().Should().Be("product.txt");
        asset.GetProperty("contentType").GetString().Should().Be("text/plain");
        asset.GetProperty("size").GetInt64().Should().Be(10);
        asset.GetProperty("localizedAltText")[0].GetProperty("altText").GetString().Should().Be("Файл товара");

        var queried = await PostGraphQl(
            """
            query Asset($id: UUID!) {
              mediaAsset(id: $id) {
                id
                fileName
                publicUrl
              }
            }
            """,
            new { id = assetId });
        queried.RootElement.GetProperty("data").GetProperty("mediaAsset").GetProperty("id").GetString()
            .Should().Be(assetId);

        var fileResponse = await factory.HttpClient.GetAsync($"/media/assets/{assetId}/file");
        fileResponse.EnsureSuccessStatusCode();
        var fileContent = await fileResponse.Content.ReadAsStringAsync();

        fileContent.Should().Be("demo media");
    }

    [Fact]
    public async Task Protected_media_mutation_should_return_access_denied_without_authentication()
    {
        factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        using var document = await PostGraphQlAllowErrors(
            """
            mutation {
              uploadMediaAsset(input: {
                fileName: "denied.txt",
                contentType: "text/plain",
                base64Content: "SGVsbG8="
              }) {
                id
              }
            }
            """);

        document.RootElement
            .GetProperty("errors")[0]
            .GetProperty("extensions")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task Search_content_items_should_use_postgresql_full_text_index()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var contentTypeId = await CreateContentType("searchProductIntegration", "Search Product Integration");
        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "searchProductIntegration", data: $data }) {
                id
              }
            }
            """,
            new { data = """{"title":"Espresso grinder"}""" });

        var reindex = await PostGraphQl(
            """
            mutation {
              reindexContentSearch {
                indexedCount
              }
            }
            """);
        reindex.RootElement.GetProperty("data").GetProperty("reindexContentSearch").GetProperty("indexedCount").GetInt32()
            .Should().BeGreaterThan(0);

        var search = await PostGraphQl(
            """
            query {
              searchContentItems(query: "espresso", contentTypeApiName: "searchProductIntegration", skip: 0, take: 10) {
                totalCount
                contentTypeFacets { value count }
                statusFacets { value count }
                items {
                  id
                  contentTypeApiName
                  data
                  score
                }
              }
            }
            """);
        var result = search.RootElement.GetProperty("data").GetProperty("searchContentItems");

        result.GetProperty("totalCount").GetInt32().Should().Be(1);
        result.GetProperty("contentTypeFacets")[0].GetProperty("value").GetString()
            .Should().Be("searchProductIntegration");
        result.GetProperty("statusFacets")[0].GetProperty("count").GetInt32()
            .Should().BeGreaterThan(0);
        result.GetProperty("items")[0].GetProperty("contentTypeApiName").GetString()
            .Should().Be("searchProductIntegration");
        result.GetProperty("items")[0].GetProperty("data").GetString()
            .Should().Contain("Espresso grinder");
        result.GetProperty("items")[0].GetProperty("score").GetDouble()
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Search_reindex_should_return_access_denied_without_administrator()
    {
        var adminAccessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        await PostGraphQl(
            """
            mutation {
              createUser(input: {
                userName: "searchReindexDeniedIntegration",
                password: "Editor1234",
                isAdministrator: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);

        var userAccessToken = await Login("searchReindexDeniedIntegration", "Editor1234");
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAccessToken);

        using var denied = await PostGraphQlAllowErrors(
            """
            mutation {
              reindexContentSearch {
                indexedCount
              }
            }
            """);

        denied.RootElement
            .GetProperty("errors")[0]
            .GetProperty("extensions")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task Search_content_items_should_require_content_read_access()
    {
        var adminAccessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        await PostGraphQl(
            """
            mutation {
              createUser(input: {
                userName: "searchReaderDeniedIntegration",
                password: "Editor1234",
                isAdministrator: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);

        var userAccessToken = await Login("searchReaderDeniedIntegration", "Editor1234");
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAccessToken);

        using var denied = await PostGraphQlAllowErrors(
            """
            query {
              searchContentItems(query: "espresso", contentTypeApiName: "searchProductIntegration", skip: 0, take: 10) {
                totalCount
              }
            }
            """);

        denied.RootElement
            .GetProperty("errors")[0]
            .GetProperty("extensions")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task Role_content_type_access_should_allow_non_admin_content_mutation()
    {
        var adminAccessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var contentTypeId = await CreateContentType("roleProductIntegration", "Role Product Integration");
        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });

        var user = await PostGraphQl(
            """
            mutation {
              createUser(input: {
                userName: "roleEditorIntegration",
                password: "Editor1234",
                isAdministrator: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);
        var userId = user.RootElement.GetProperty("data").GetProperty("createUser").GetProperty("id").GetString();

        var role = await PostGraphQl(
            """
            mutation {
              createRole(input: {
                name: "roleProductEditorIntegration",
                displayName: "Role Product Editor Integration",
                preset: EDITOR,
                canCreateContentTypes: false,
                canViewGraphQLSchema: false
              }) { id preset }
            }
            """);
        var roleId = role.RootElement.GetProperty("data").GetProperty("createRole").GetProperty("id").GetString();

        await PostGraphQl(
            """
            mutation GrantRole($roleId: UUID!) {
              grantRoleContentTypeAccess(input: {
                roleId: $roleId,
                contentTypeApiName: "roleProductIntegration",
                canManageSchema: false,
                canManageItems: true,
                canReadItems: true
              }) { id }
            }
            """,
            new { roleId });
        await PostGraphQl(
            """
            mutation Assign($userId: UUID!, $roleId: UUID!) {
              assignRoleToUser(input: { userId: $userId, roleId: $roleId }) { id }
            }
            """,
            new { userId, roleId });

        var editorAccessToken = await Login("roleEditorIntegration", "Editor1234");
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", editorAccessToken);

        var created = await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "roleProductIntegration", data: $data }) {
                id
                contentTypeApiName
              }
            }
            """,
            new { data = """{"title":"Role item"}""" });

        created.RootElement
            .GetProperty("data")
            .GetProperty("createContentItem")
            .GetProperty("contentTypeApiName")
            .GetString()
            .Should()
            .Be("roleProductIntegration");
    }

    [Fact]
    public async Task Service_account_token_should_allow_content_mutation_through_role()
    {
        var adminAccessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var contentTypeId = await CreateContentType("serviceProductIntegration", "Service Product Integration");
        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });

        var serviceAccount = await PostGraphQl(
            """
            mutation {
              createServiceAccount(input: {
                name: "catalogImporterIntegration",
                displayName: "Catalog Importer Integration",
                canViewGraphQLSchema: false
              }) { id isServiceAccount }
            }
            """);
        var serviceAccountId = serviceAccount.RootElement
            .GetProperty("data")
            .GetProperty("createServiceAccount")
            .GetProperty("id")
            .GetString();
        serviceAccount.RootElement
            .GetProperty("data")
            .GetProperty("createServiceAccount")
            .GetProperty("isServiceAccount")
            .GetBoolean()
            .Should()
            .BeTrue();

        var role = await PostGraphQl(
            """
            mutation {
              createRole(input: {
                name: "serviceProductImporterIntegration",
                displayName: "Service Product Importer Integration",
                preset: EDITOR,
                canCreateContentTypes: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);
        var roleId = role.RootElement.GetProperty("data").GetProperty("createRole").GetProperty("id").GetString();

        await PostGraphQl(
            """
            mutation GrantRole($roleId: UUID!) {
              grantRoleContentTypeAccess(input: {
                roleId: $roleId,
                contentTypeApiName: "serviceProductIntegration",
                canManageSchema: false,
                canManageItems: true,
                canReadItems: true
              }) { id }
            }
            """,
            new { roleId });
        await PostGraphQl(
            """
            mutation Assign($serviceAccountId: UUID!, $roleId: UUID!) {
              assignRoleToUser(input: { userId: $serviceAccountId, roleId: $roleId }) { id }
            }
            """,
            new { serviceAccountId, roleId });

        var tokenResult = await PostGraphQl(
            """
            mutation Token($serviceAccountId: UUID!) {
              createServiceAccountToken(input: {
                serviceAccountId: $serviceAccountId,
                name: "integration-test-token",
                lifetimeDays: 365
              }) {
                accessToken
                serviceAccountId
                name
              }
            }
            """,
            new { serviceAccountId });
        var serviceAccountToken = tokenResult.RootElement
            .GetProperty("data")
            .GetProperty("createServiceAccountToken")
            .GetProperty("accessToken")
            .GetString();

        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceAccountToken);

        var created = await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "serviceProductIntegration", data: $data }) {
                id
                contentTypeApiName
              }
            }
            """,
            new { data = """{"title":"Imported item"}""" });

        created.RootElement
            .GetProperty("data")
            .GetProperty("createContentItem")
            .GetProperty("contentTypeApiName")
            .GetString()
            .Should()
            .Be("serviceProductIntegration");
    }

    [Fact]
    public async Task Field_locale_role_access_should_limit_content_mutation_fields()
    {
        var adminAccessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var contentTypeId = await CreateContentType("localizedArticleIntegration", "Localized Article Integration");
        await PostGraphQl(
            """
            mutation AddTitle($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: LOCALIZED,
                isRequired: false
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation AddNotes($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "internalNotes",
                displayName: "Internal Notes",
                kind: TEXT,
                localization: NON_LOCALIZED,
                isRequired: false
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });

        var user = await PostGraphQl(
            """
            mutation {
              createUser(input: {
                userName: "ruTitleEditorIntegration",
                password: "Editor1234",
                isAdministrator: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);
        var userId = user.RootElement.GetProperty("data").GetProperty("createUser").GetProperty("id").GetString();

        var role = await PostGraphQl(
            """
            mutation {
              createRole(input: {
                name: "ruTitleEditorIntegration",
                displayName: "RU Title Editor Integration",
                preset: EDITOR,
                canCreateContentTypes: false,
                canViewGraphQLSchema: false
              }) { id }
            }
            """);
        var roleId = role.RootElement.GetProperty("data").GetProperty("createRole").GetProperty("id").GetString();

        await PostGraphQl(
            """
            mutation GrantRole($roleId: UUID!) {
              grantRoleContentTypeAccess(input: {
                roleId: $roleId,
                contentTypeApiName: "localizedArticleIntegration",
                fieldApiName: "title",
                locale: "ru",
                canManageSchema: false,
                canManageItems: true,
                canReadItems: true
              }) { id }
            }
            """,
            new { roleId });
        await PostGraphQl(
            """
            mutation Assign($userId: UUID!, $roleId: UUID!) {
              assignRoleToUser(input: { userId: $userId, roleId: $roleId }) { id }
            }
            """,
            new { userId, roleId });

        var editorAccessToken = await Login("ruTitleEditorIntegration", "Editor1234");
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", editorAccessToken);

        await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "localizedArticleIntegration", data: $data }) {
                id
              }
            }
            """,
            new { data = """{"title":{"ru":"Можно"}}""" });

        var denied = async () => await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "localizedArticleIntegration", data: $data }) {
                id
              }
            }
            """,
            new { data = """{"title":{"en":"Denied"}}""" });

        await denied.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ACCESS_DENIED*");
    }

    [Fact]
    public async Task Dynamic_published_content_type_queries_should_be_available_at_startup()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var created = await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "dynamicProduct", data: $data }) {
                id
              }
            }
            """,
            new { data = """{"title":"Dynamic Phone"}""" });
        var itemId = created.RootElement
            .GetProperty("data")
            .GetProperty("createContentItem")
            .GetProperty("id")
            .GetString();

        var collection = await PostGraphQl(
            """
            query DynamicCollection {
              dynamicProducts(search: "Dynamic", orderBy: "createdAt", descending: true, skip: 0, take: 10) {
                id
                contentTypeApiName
                title
              }
            }
            """);
        var items = collection.RootElement
            .GetProperty("data")
            .GetProperty("dynamicProducts");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("title").GetString().Should().Be("Dynamic Phone");

        var single = await PostGraphQl(
            """
            query DynamicSingle($id: UUID!) {
              dynamicProduct(id: $id) {
                id
                contentTypeApiName
                title
              }
            }
            """,
            new { id = itemId });
        var item = single.RootElement.GetProperty("data").GetProperty("dynamicProduct");

        item.GetProperty("id").GetString().Should().Be(itemId);
        item.GetProperty("contentTypeApiName").GetString().Should().Be("dynamicProduct");
        item.GetProperty("title").GetString().Should().Be("Dynamic Phone");
    }

    [Fact]
    public async Task Publishing_content_type_should_rebuild_dynamic_graphql_schema()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var contentTypeId = await CreateContentType("liveArticle", "Live Article");
        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "liveArticle", data: $data }) {
                id
              }
            }
            """,
            new { data = """{"title":"Fresh schema"}""" });

        factory.Services
            .GetRequiredService<DynamicContentSchemaProvider>()
            .Snapshot
            .ContentTypes
            .Should()
            .Contain(contentType => contentType.ApiName == "liveArticle");
        factory.Services
            .GetRequiredService<DynamicContentTypeModule>()
            .Snapshot
            .ContentTypes
            .Should()
            .Contain(contentType => contentType.ApiName == "liveArticle");

        var collection = await PostGraphQlWithSchemaRetry(
            """
            query LiveArticles {
              liveArticles {
                title
              }
            }
            """);

        collection.RootElement
            .GetProperty("data")
            .GetProperty("liveArticles")[0]
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Fresh schema");
    }


    [Fact]
    public async Task Content_item_lifecycle_should_update_version_publish_unpublish_archive_and_page()
    {
        var accessToken = await LoginAsAdmin();
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var contentTypeId = await CreateContentType("articleIntegration", "Article Integration");
        await PostGraphQl(
            """
            mutation AddField($contentTypeId: UUID!) {
              addContentField(input: {
                contentTypeId: $contentTypeId,
                apiName: "title",
                displayName: "Title",
                kind: STRING,
                localization: NON_LOCALIZED,
                isRequired: true
              }) { contentTypeId }
            }
            """,
            new { contentTypeId });
        await PostGraphQl(
            """
            mutation Publish($contentTypeId: UUID!) {
              publishContentType(input: { contentTypeId: $contentTypeId }) { id }
            }
            """,
            new { contentTypeId });

        var created = await PostGraphQl(
            """
            mutation Create($data: String!) {
              createContentItem(input: { contentTypeApiName: "articleIntegration", data: $data }) {
                id
                status
              }
            }
            """,
            new { data = """{"title":"Draft"}""" });
        var itemId = created.RootElement.GetProperty("data").GetProperty("createContentItem").GetProperty("id").GetString();

        var updated = await PostGraphQl(
            """
            mutation Update($id: UUID!, $data: String!) {
              updateContentItem(input: { id: $id, data: $data }) {
                id
                data
              }
            }
            """,
            new { id = itemId, data = """{"title":"Updated"}""" });
        updated.RootElement.GetProperty("data").GetProperty("updateContentItem").GetProperty("data").GetString()
            .Should().Contain("Updated");

        var published = await PostGraphQl(
            """
            mutation PublishItem($id: UUID!) {
              publishContentItem(input: { id: $id }) { status }
            }
            """,
            new { id = itemId });
        published.RootElement.GetProperty("data").GetProperty("publishContentItem").GetProperty("status").GetString()
            .Should().Be("PUBLISHED");

        var unpublished = await PostGraphQl(
            """
            mutation UnpublishItem($id: UUID!) {
              unpublishContentItem(input: { id: $id }) { status }
            }
            """,
            new { id = itemId });
        unpublished.RootElement.GetProperty("data").GetProperty("unpublishContentItem").GetProperty("status").GetString()
            .Should().Be("DRAFT");

        var page = await PostGraphQl(
            """
            query Page {
              contentItemsPage(contentTypeApiName: "articleIntegration", status: DRAFT, search: "Updated", orderBy: "updatedAt", descending: true, skip: 0, take: 10) {
                totalCount
                items { id status }
              }
            }
            """);
        page.RootElement.GetProperty("data").GetProperty("contentItemsPage").GetProperty("totalCount").GetInt32()
            .Should().Be(1);

        var versions = await PostGraphQl(
            """
            query Versions($id: UUID!) {
              contentItemVersions(id: $id) {
                version
                status
                data
              }
            }
            """,
            new { id = itemId });
        versions.RootElement.GetProperty("data").GetProperty("contentItemVersions").GetArrayLength()
            .Should().BeGreaterThanOrEqualTo(3);

        var archived = await PostGraphQl(
            """
            mutation ArchiveItem($id: UUID!) {
              archiveContentItem(input: { id: $id }) { status }
            }
            """,
            new { id = itemId });
        archived.RootElement.GetProperty("data").GetProperty("archiveContentItem").GetProperty("status").GetString()
            .Should().Be("ARCHIVED");
    }

    private async Task<string> LoginAsAdmin()
        => await Login("admin", factory.AdminPassword);

    private async Task<string> Login(string userName, string password)
    {
        var document = await PostGraphQl(
            """
            mutation Login($userName: String!, $password: String!) {
              login(input: { userName: $userName, password: $password }) {
                accessToken
              }
            }
            """,
            new { userName, password });

        return document.RootElement
            .GetProperty("data")
            .GetProperty("login")
            .GetProperty("accessToken")
            .GetString()!;
    }

    private async Task<string> CreateContentType(string apiName, string displayName)
    {
        var document = await PostGraphQl(
            """
            mutation CreateType($apiName: String!, $displayName: String!) {
              createContentType(input: { apiName: $apiName, displayName: $displayName }) {
                id
              }
            }
            """,
            new { apiName, displayName });

        return document.RootElement
            .GetProperty("data")
            .GetProperty("createContentType")
            .GetProperty("id")
            .GetString()!;
    }

    private async Task<JsonDocument> PostGraphQl(string query, object? variables = null)
    {
        var response = await factory.HttpClient.PostAsJsonAsync(
            "/graphql",
            new GraphQlRequest(query, variables));
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
        }

        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        if (document.RootElement.TryGetProperty("errors", out var errors))
        {
            throw new InvalidOperationException(errors.ToString());
        }

        return document;
    }

    private async Task<JsonDocument> PostGraphQlAllowErrors(string query, object? variables = null)
    {
        var response = await factory.HttpClient.PostAsJsonAsync(
            "/graphql",
            new GraphQlRequest(query, variables));
        response.EnsureSuccessStatusCode();

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    private async Task<JsonDocument> PostGraphQlWithSchemaRetry(string query, object? variables = null)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                return await PostGraphQl(query, variables);
            }
            catch (HttpRequestException exception) when (exception.Message.Contains("does not exist on the type `Query`", StringComparison.Ordinal))
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        throw lastException ?? new InvalidOperationException("GraphQL schema was not rebuilt.");
    }

    private sealed record GraphQlRequest(string Query, object? Variables = null);
}
