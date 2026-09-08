using QRCoder;

namespace QigloRestaurant.Web.Services;

public interface IQrCodeService
{
    byte[] CreatePng(string value);
}

public sealed class QrCodeService : IQrCodeService
{
    public byte[] CreatePng(string value)
    {
        using var data = QRCodeGenerator.GenerateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(12, drawQuietZones: true);
    }
}
