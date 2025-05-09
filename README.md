# Gerador e Validador de Boleto

Este é um projeto completo para geração e validação de boletos, composto por uma interface web amigável e APIs Azure Functions para processamento backend.

## Estrutura do Projeto

```
geradorDeBoleto/
├── front/                 # Interface web do usuário
├── fnValidaBoleto/       # API de validação de boletos
└── geradorDeBoleto/      # API de geração de boletos
```

## Componentes

### Frontend (front/)

Interface web responsiva que permite:
- Entrada de dados para geração de novos boletos
- Validação de boletos existentes
- Visualização de resultados em formato amigável

### API de Validação (fnValidaBoleto/)

Azure Function responsável por:
- Validação da estrutura do código de barras
- Verificação da autenticidade do boleto
- Retorno de informações detalhadas sobre o boleto

### API de Geração (geradorDeBoleto/)

Azure Function que:
- Gera novos boletos baseados nos parâmetros fornecidos
- Calcula dígitos verificadores
- Retorna o código de barras e linha digitável

## Como Usar

### Frontend

1. Navegue até a pasta `front/`
2. Abra o arquivo `index.html` em um navegador
3. Para validar um boleto:
   - Digite o código de barras no campo apropriado
   - Clique em "Validar"
4. Para gerar um novo boleto:
   - Preencha os campos necessários
   - Clique em "Gerar"

### APIs (Ambiente de Desenvolvimento)

1. Certifique-se de ter o .NET 8.0 SDK instalado
2. Para executar a API de validação:
   ```powershell
   cd fnValidaBoleto
   func start
   ```
3. Para executar a API de geração:
   ```powershell
   cd geradorDeBoleto
   func start
   ```

## Tecnologias Utilizadas

- Frontend: HTML5, CSS3, JavaScript
- Backend: .NET 8.0, Azure Functions
- Cloud: Microsoft Azure

## Requisitos

- .NET 8.0 SDK
- Azure Functions Core Tools
- Navegador web moderno
- Conta Azure (para deploy)

## Licença

Este projeto está licenciado sob a Licença MIT.