using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetQRCode;

internal sealed record GetQRCodeQuery(string EventSlug, Guid RegistrationId) : Query<GetQRCodeResult>;

internal sealed record GetQRCodeResult(byte[] Content, string ContentType, string FileName);
