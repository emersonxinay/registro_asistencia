using QRCoder;

namespace registroAsistencia.Services;

public interface IQrService
{
    string GenerateBase64Qr(string text);
    byte[] GeneratePngBytes(string text);
}

public class QrService : IQrService
{
    public string GenerateBase64Qr(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        byte[] bytes = png.GetGraphic(20);
        return Convert.ToBase64String(bytes);
    }

    public byte[] GeneratePngBytes(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(20);
    }
}