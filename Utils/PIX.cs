using System.Globalization;
using System.Text;
using static PIX_Qrcode.Models.PIXModel;
using System.Text.RegularExpressions;

namespace PIX_Qrcode.Utils
{
    public class PIX
    {
        public async Task<RetornoValidacaoPixModel> ValidaRequisicaoAsync(RequestModel model)
        {
            model.nomeFavorecido = PadronizarString(model.nomeFavorecido ?? "");
            model.cidadeFavorecido = PadronizarString(model.cidadeFavorecido ?? "");
            model.mensagemDestinatario = PadronizarString(model.mensagemDestinatario ?? "");

            if (!ushort.TryParse(model.moedaAReceber ?? "986", out ushort resultado)) // se vier null, assume como sendo "986", que representa R$
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "A moeda a receber deve estar de acordo com a ISO 4217. Omitir ou informar 986 caso seja Real." });
            }

            if (string.IsNullOrEmpty(model.chaveFavorecido))
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "É necessário informar a chave do favorecido que irá receber o PIX." });
            }

            model.chaveFavorecido = model.chaveFavorecido.Trim();

            if (model.chaveFavorecido.Length > 77)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "A chave do favorecido pode ter no máximo 77 caracteres." });
            }

            if (string.IsNullOrEmpty(model.nomeFavorecido))
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "É necessário informar o nome do favorecido que receberá o PIX." });
            }

            if (model.nomeFavorecido.Length > 25)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "O nome do favorecido que irá receber o PIX pode ter no máximo 25 caracteres." });
            }

            if (model.valorAReceber <= 0)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "O valor a receber deve ser maior que zero." });
            }

            if (ValidaSiglaPais(model.siglaPais ?? "BR") == false) // se vier null assume "BR" que representa Brasil
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "A sigla do país pode conter somente 2 caracteres e deve estar de acordo com a ISO 3166-1 alpha-2. Omitir ou informar BR caso seja Brasil." });
            }

            if (string.IsNullOrEmpty(model.cidadeFavorecido))
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "É necessário informar o nome da cidade em até 15 caracteres do favorecido que receberá o PIX." });
            }

            if (model.cidadeFavorecido.Length > 15)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "O nome da cidade do favorecido que irá receber o PIX pode ter no máximo 15 caracteres." });
            }

            if (string.IsNullOrEmpty(model.identificador))
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "Para critérios de rastreabilidade é necessário atribuir um identificador de até 25 caracteres para toda transação com PIX." });
            }

            if (model.identificador.Length > 25)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "O identificador da transação do PIX pode ter no máximo 25 caracteres." });
            }

            if (await ValidarTXIDAsync(model.identificador) == false)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "O identificador da transação do PIX pode conter apenas números e letras (exceto cedilha) sem acentuação." });
            }

            if (model.mensagemDestinatario?.Length > 35)
            {
                return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = false, mensagem = "A mensagem ao destinatário não é obrigatória, mas caso queira informar, ela pode ter no máximo 35 caracteres." });
            }

            return await Task.FromResult(new RetornoValidacaoPixModel() { sucesso = true, mensagem = "Condições para geração de QRCode PIX satisfeitas.", request = model });
        }

        public async Task<string> MontaPayloadPIX(RequestModel model)
        {

            if (string.IsNullOrEmpty(model.moedaAReceber))
            {
                model.moedaAReceber = "986";
            }

            if (string.IsNullOrEmpty(model.siglaPais))
            {
                model.siglaPais = "BR";
            }
            else
            {
                model.siglaPais = model.siglaPais.ToUpper();
            }

            var cultureInfo = new CultureInfo("en-US");
            string comprimentoStringChaveFavorecido = model.chaveFavorecido.Length.ToString("D2"); // comprimento da chave do favorecido com zero a esquerda caso seja menor que 10 caracteres
            string comprimentoStringNomeFavorecido = model.nomeFavorecido.Length.ToString("D2");
            string valorAReceber = model.valorAReceber.ToString("0.00", cultureInfo); // se nao declarar a culture info ele puxa virgula no lugar do ponto e da problemas no pix
            string comprimentoStringValorAReceber = valorAReceber.Length.ToString("D2");
            string comprimentoStringMoedaAReceber = model.moedaAReceber.Length.ToString("D2");
            string comprimentoStringSiglaPais = model.siglaPais.Length.ToString("D2");

            string comprimentoStringIdentificadorTransacao = model.identificador.Length.ToString("D2"); // comprimento apenas do identificador
            string aditionalData = "05" + comprimentoStringIdentificadorTransacao + model.identificador; // contém o "05" além dos dois caracteres que representam o comprimento do identificador e também o identificador em si
            string comprimentoStringAditionalData = aditionalData.Length.ToString("D2");

            string comprimentoStringMensagemDestinatario = model.mensagemDestinatario.Length.ToString("D2");
            string comprimentoStringCidadeFavorecido = model.cidadeFavorecido.Length.ToString("D2");

            string inicioPayloadPIX = "00020126"; // padrão inicial para caracterizar PIX
            string bancoCentralGUI = "0014br.gov.bcb.pix"; // padrão pix
            string naoInformado = "52040000"; // no manual do pix também consta como não informado
            string crc16Padrao = "6304"; // identificador do crc16 que é o calculo da string

            if (!string.IsNullOrEmpty(model.mensagemDestinatario))
            {
                model.mensagemDestinatario = "02" + comprimentoStringMensagemDestinatario + model.mensagemDestinatario; // monta a mensagem inteira aqui caso exista
            }
            else
            {
                model.mensagemDestinatario = "";
            }
            string comprimentoStringInicioPayload = ("01" + comprimentoStringNomeFavorecido + model.chaveFavorecido + bancoCentralGUI + model.mensagemDestinatario).Length.ToString("D2"); // aqui já estão sendo contabilizados alguns caracteres a mais "01" o comprimento da chave e a mensagem ao destinatário e seu comprimento caso exista

            StringBuilder sbPayload = new StringBuilder();
            sbPayload.Append(inicioPayloadPIX);
            sbPayload.Append(comprimentoStringInicioPayload);
            sbPayload.Append(bancoCentralGUI);

            sbPayload.Append("01");
            sbPayload.Append(comprimentoStringChaveFavorecido);
            sbPayload.Append(model.chaveFavorecido);

            if (comprimentoStringMensagemDestinatario != "00") // somente inclui a mensagem caso ela possua conteúdo (seja maior do que 00)
            {
                sbPayload.Append(model.mensagemDestinatario);
            }

            sbPayload.Append(naoInformado);

            sbPayload.Append("53");
            sbPayload.Append(comprimentoStringMoedaAReceber);
            sbPayload.Append(model.moedaAReceber);

            sbPayload.Append("54");
            sbPayload.Append(comprimentoStringValorAReceber);
            sbPayload.Append(valorAReceber);

            sbPayload.Append("58");
            sbPayload.Append(comprimentoStringSiglaPais);
            sbPayload.Append(model.siglaPais);

            sbPayload.Append("59");
            sbPayload.Append(comprimentoStringNomeFavorecido);
            sbPayload.Append(model.nomeFavorecido);

            sbPayload.Append("60");
            sbPayload.Append(comprimentoStringCidadeFavorecido);
            sbPayload.Append(model.cidadeFavorecido);

            sbPayload.Append("62");
            sbPayload.Append(comprimentoStringAditionalData);
            sbPayload.Append(aditionalData);

            sbPayload.Append(crc16Padrao);

            string pixPayload = sbPayload.ToString();

            return pixPayload + await CalcularCRC16Async(pixPayload);
        }

        public async Task<string> CalcularCRC16Async(string dados)
        {
            return await Task.Run(() =>
            {
                ushort resultado = 0xFFFF;

                foreach (char c in dados)
                {
                    resultado ^= (ushort)(c << 8);

                    for (int i = 0; i < 8; i++)
                    {
                        if ((resultado & 0x8000) != 0)
                        {
                            resultado = (ushort)((resultado << 1) ^ 0x1021);
                        }
                        else
                        {
                            resultado <<= 1;
                        }
                        resultado &= 0xFFFF;
                    }
                }

                // Converte resultado para string hexadecimal com 4 caracteres
                string crc16Hex = resultado.ToString("X4");

                return crc16Hex;
            });
        }

        public async Task<bool> ValidarTXIDAsync(string input)
        {
            await Task.Yield();

            Regex regex = new Regex(@"^[a-zA-Z0-9]+$");

            return regex.IsMatch(input);
        }


        private static readonly HashSet<string> validCountryCodes = new HashSet<string>
    {
        "AD", "AE", "AF", "AG", "AI", "AL", "AM", "AO", "AQ", "AR", "AS", "AT", "AU", "AW", "AZ",
        "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BL", "BM", "BN", "BO", "BQ", "BR",
        "BS", "BT", "BW", "BY", "BZ", "CA", "CC", "CD", "CF", "CG", "CH", "CI", "CK", "CL", "CM",
        "CN", "CO", "CR", "CU", "CV", "CW", "CX", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ",
        "EC", "EE", "EG", "EH", "ER", "ES", "ET", "FI", "FJ", "FK", "FM", "FO", "FR", "GA", "GB",
        "GD", "GE", "GF", "GG", "GH", "GI", "GL", "GM", "GN", "GP", "GQ", "GR", "GS", "GU", "GT",
        "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IM", "IN", "IO", "IQ", "IR",
        "IS", "IT", "JE", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KP", "KR", "KW",
        "KY", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC",
        "MD", "ME", "MF", "MG", "MH", "MK", "ML", "MM", "MN", "MO", "MP", "MQ", "MR", "MS", "MT",
        "MU", "MV", "MW", "MY", "MX", "MZ", "NA", "NC", "NE", "NF", "NG", "NI", "NL", "NO", "NP",
        "NR", "NU", "NZ", "OM", "PA", "PE", "PF", "PG", "PH", "PK", "PL", "PM", "PN", "PR", "PS",
        "PT", "PW", "PY", "QA", "RE", "RO", "RS", "RU", "RW", "SB", "SH", "SA", "SC", "SD", "SE",
        "SG", "SI", "SK", "SL", "SM", "SN", "SO", "SR", "SS", "ST", "SV", "SX", "SY", "SZ", "TC",
        "TD", "TF", "TG", "TH", "TJ", "TK", "TL", "TM", "TN", "TO", "TR", "TT", "TW", "TV", "TZ",
        "UA", "UG", "US", "UY", "UZ", "VA", "VC", "VE", "VG", "VN", "VU", "VI", "WF", "WS", "XK",
        "YE", "YT", "ZA", "ZM", "ZW"
    };

        public static bool ValidaSiglaPais(string input)
        {
            return validCountryCodes.Contains(input.ToUpper());
        }

        public static string PadronizarString(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            string normalizedString = texto.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();
            bool ultimoCaracterEraEspaco = false;

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                    (Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c)))
                {
                    if (c == ' ')
                    {
                        if (!ultimoCaracterEraEspaco)
                        {
                            stringBuilder.Append(c);
                        }
                        ultimoCaracterEraEspaco = true;
                    }
                    else
                    {
                        stringBuilder.Append(c);
                        ultimoCaracterEraEspaco = false;
                    }
                }
            }

            string resultado = stringBuilder.ToString().Trim();

            return resultado;
        }

    }
}
