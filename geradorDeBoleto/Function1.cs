using Azure.Messaging.ServiceBus;
using BarcodeStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnGeradorBoletos
{
    public class GeradorCodigoBarras(ILogger<GeradorCodigoBarras> logger)
    {
        private readonly ILogger<GeradorCodigoBarras> _logger = logger;
        private readonly string _serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString") 
            ?? throw new InvalidOperationException("ServiceBusConnectionString environment variable is not set.");
        private readonly string _queueName = "gerador-de-boleto-queue";

        [Function("barcode-generate")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                string valor = data?.valor;
                string dataVencimento = data?.dataVencimento;

                string barcodeData;

                //Validação dos dados
                if (string.IsNullOrEmpty(valor) || string.IsNullOrEmpty(dataVencimento))
                {
                    return new BadRequestObjectResult("Os campos valor e dataVencimento são obrigatórios");
                }

                //Validar formato da Data de Vencimento YYYY-MM-DD
                if (!DateTime.TryParseExact(dataVencimento, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
                {
                    return new BadRequestObjectResult("Data de Vencimento inválida");
                }

                string dateStr = dateObj.ToString("yyyyMMdd");

                if (!decimal.TryParse(valor, out decimal valorDecimal))
                {
                    return new BadRequestObjectResult("Valor inválido");
                }
                int valorCentavos = (int)(valorDecimal * 10);
                string valorStr = valorCentavos.ToString("D8");

                string bankCode = "059";
                string baseCode = string.Concat(bankCode, dateStr, valorStr);
               
                barcodeData = baseCode.Length < 44 ? baseCode.PadRight(44, '0') : baseCode.Substring(0, 44);
                _logger.LogInformation($"Barcode gerado: {barcodeData}");

                Barcode barcode = new Barcode();
                var skImage = barcode.Encode(BarcodeStandard.Type.Code128, barcodeData);

                using (var encodeData = skImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    var imageBytes = encodeData.ToArray();

                    string base64String = Convert.ToBase64String(imageBytes);

                    var resultObject = new
                    {
                        barcode = barcodeData,
                        valorOriginal = valorDecimal,
                        DataVencimento = DateTime.Now.AddDays(5),
                        ImagemBase64 = base64String
                    };

                    await SendFileFallback(resultObject, _serviceBusConnectionString, _queueName);

                    return new OkObjectResult(resultObject);
                }

            }
            catch (Exception ex)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }

        private async Task SendFileFallback(object resultObject, string serviceBusConnectionString, string queueName)
        {
            await using var client = new ServiceBusClient(serviceBusConnectionString);

            ServiceBusSender sender = client.CreateSender(queueName);

            string messageBody = JsonConvert.SerializeObject(resultObject);

            ServiceBusMessage message = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(message);

            _logger.LogInformation($"Mensagem enviada para a fila {queueName}");

        }
    }
}