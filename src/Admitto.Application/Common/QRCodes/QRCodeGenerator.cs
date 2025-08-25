using QRCoder;

namespace Amolenk.Admitto.Application.Common.QRCodes;

public class QRCodeGenerator
{
    public byte[] GenerateRegistrationQRCode(Guid registrationId)
    {
        return GenerateQRCode(registrationId.ToString());
    }
    
    private byte[] GenerateQRCode(string content)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.Q);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}