namespace PIX_Qrcode.Models
{
    public class PIXModel
    {
        public class RequestModel
        {
            public string? chaveFavorecido { get; set; }
            public string? nomeFavorecido { get; set; }
            public decimal valorAReceber { get; set; }
            public string? moedaAReceber { get; set; }
            public string? siglaPais { get; set; }
            public string? cidadeFavorecido { get; set; }
            public string? identificador { get; set; }
            public string? mensagemDestinatario { get; set; }
        }

        public class RetornoValidacaoPixModel
        {
            public bool sucesso { get; set; }
            public string mensagem { get; set; }
            public RequestModel request { get; set; }
        }
    }
}
