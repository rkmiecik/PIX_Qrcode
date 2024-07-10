using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using static PIX_Qrcode.Models.PIXModel;

namespace PIX_Qrcode.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PIXController : ControllerBase
    {
        [HttpPost("[action]")]
        public async Task<IActionResult> GerarQrCodeAsync([FromBody] RequestModel model)
        {
            RetornoValidacaoPixModel validacaoPIXRequest = await new Utils.PIX().ValidaRequisicaoAsync(model);
            if (validacaoPIXRequest.sucesso == false)
            {
                return StatusCode(200, validacaoPIXRequest);
            }

            try
            {
                string payload = await new Utils.PIX().MontaPayloadPIX(validacaoPIXRequest.request);
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.H)) 
                {
                    var qrCode = new PngByteQRCode(qrCodeData);
                    var qrCodeBytes = qrCode.GetGraphic(20);


                    using (var ms = new MemoryStream(qrCodeBytes))
                    using (var originalBitmap = new Bitmap(ms))
                    {
                        var qrCodeImage = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);
                        using (var graphics = Graphics.FromImage(qrCodeImage))
                        {
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, qrCodeImage.Width, qrCodeImage.Height));
                        }

                        using (var logo = Image.FromFile("Images/logo.png"))
                        {
                            int logoSize = qrCodeImage.Width / 5;
                            int logoX = (qrCodeImage.Width - logoSize) / 2;
                            int logoY = (qrCodeImage.Height - logoSize) / 2;

                            using (var graphics = Graphics.FromImage(qrCodeImage))
                            {
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                                using (var path = new GraphicsPath())
                                {
                                    path.AddEllipse(logoX, logoY, logoSize, logoSize);
                                    graphics.FillPath(Brushes.White, path);
                                }

                                graphics.DrawImage(logo, new Rectangle(logoX, logoY, logoSize, logoSize));
                            }

                            using (var resultMs = new MemoryStream())
                            {
                                qrCodeImage.Save(resultMs, ImageFormat.Png);
                                return File(resultMs.ToArray(), "image/png");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(200, new RetornoValidacaoPixModel() { sucesso = false, mensagem = ex.Message });
            }
        }
    }
}
