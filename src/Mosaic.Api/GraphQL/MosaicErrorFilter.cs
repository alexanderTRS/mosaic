using HotChocolate;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Media.Application.MediaAssets;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Api.GraphQL;

public sealed class MosaicErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            AccessDeniedException exception => error
                .WithMessage(exception.Message)
                .WithCode("ACCESS_DENIED"),
            LoginFailedException exception => error
                .WithMessage(exception.Message)
                .WithCode("LOGIN_FAILED"),
            PasswordPolicyViolationException exception => error
                .WithMessage(exception.Message)
                .WithCode("PASSWORD_POLICY_VIOLATION"),
            DomainRuleViolationException exception => error
                .WithMessage(exception.Message)
                .WithCode("DOMAIN_RULE_VIOLATION"),
            ContentTypeNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("CONTENT_TYPE_NOT_FOUND"),
            ContentTypeSchemaNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("CONTENT_TYPE_NOT_FOUND"),
            ContentItemNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("CONTENT_ITEM_NOT_FOUND"),
            MediaAssetNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("MEDIA_ASSET_NOT_FOUND"),
            InvalidOperationException exception => error
                .WithMessage(exception.Message)
                .WithCode("INVALID_OPERATION"),
            _ => error
        };
    }
}
