// using Amolenk.Admitto.ApiService.Middleware;
// using Amolenk.Admitto.Application.UseCases.Email.ClearEventEmailTemplate;
// using Amolenk.Admitto.Application.UseCases.Email.ClearTeamEmailTemplate;
// using Amolenk.Admitto.Application.UseCases.Email.ConfigureEventEmailTemplate;
// using Amolenk.Admitto.Application.UseCases.Email.ConfigureTeamEmailTemplate;
// using Amolenk.Admitto.Application.UseCases.Email.GetEventEmailTemplates;
// using Amolenk.Admitto.Application.UseCases.Email.GetTeamEmailTemplates;
// using Amolenk.Admitto.Application.UseCases.Email.Reconfirmation.PreviewReconfirmationEmails;
// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
// using Amolenk.Admitto.Application.UseCases.Email.TestEmail;
//
// namespace Amolenk.Admitto.ApiService.Endpoints;
//
// public static class BulkEmailEndpoints
// {
//     public static void MapBulkEmailEndpoints(this IEndpointRouteBuilder app)
//     {
//         var group = app.MapGroup("/")
//             .WithTags("Bulk Email")
//             .AddEndpointFilter<ValidationFilter>()
//             .AddEndpointFilter<UnitOfWorkFilter>()
//             .ProducesValidationProblem()
//             .ProducesProblem(StatusCodes.Status401Unauthorized)
//             .ProducesProblem(StatusCodes.Status403Forbidden)
//             .ProducesProblem(StatusCodes.Status409Conflict)
//             .ProducesProblem(StatusCodes.Status500InternalServerError)
//             .RequireAuthorization();
//
//         // group
//         //     .MapPreviewReconfirmationEmails();
//     }
// }