Geração de Qrcode para transações PIX através de API rest com C# .NET Core 8.
O intuito deste código é demonstrar a forma como é construída a estrutura do payload do PIX e a forma de transformá-lo em imagem para disponibilizá-lo para escanear.
A obtenção da imagem do Qrcode é feita atravé de uma requisição Rest com método Post, informando no requestbody os seguintes atributos:
`{
  "chaveFavorecido": "string",
  "nomeFavorecido": "string",
  "valorAReceber": 0,
  "moedaAReceber": "string",
  "siglaPais": "string",
  "cidadeFavorecido": "string",
  "identificador": "string",
  "mensagemDestinatario": "string"
}`
