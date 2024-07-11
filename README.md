Geração de Qrcode para transações PIX através de API rest com C# .NET Core 8.<br/>
O intuito deste código é demonstrar a forma como é construída a estrutura do payload do PIX e a forma de transformá-lo em imagem para disponibilizá-lo para escanear.<br/>
A obtenção da imagem do Qrcode é feita atravé de uma requisição Rest com método Post, informando no requestbody os seguintes atributos:<br/>
`{<br/>
  "chaveFavorecido": "string",<br/>
  "nomeFavorecido": "string",<br/>
  "valorAReceber": 0,<br/>
  "moedaAReceber": "string",<br/>
  "siglaPais": "string",<br/>
  "cidadeFavorecido": "string",<br/>
  "identificador": "string",<br/>
  "mensagemDestinatario": "string"<br/>
}`
