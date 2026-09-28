// ==========================
// Configuração e credenciais
// ==========================
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;

IConfigurationRoot config =
    new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

var credential = new ApiKeyCredential(
    config["PAT_TOKEN"]
    ?? throw new InvalidOperationException("Falta a definição do token: PAT_TOKEN")
);

var options = new OpenAIClientOptions
{
    Endpoint = new Uri("https://models.github.ai/inference")
};

Console.WriteLine("\n### Usando um Agente Multimodal para Analisar Imagens ###\n");

// ==========================
// Criação do Chat Client
// ==========================

IChatClient client =
    new OpenAIClient(credential, options)
        .GetChatClient("openai/gpt-4.1")
        .AsIChatClient();

// ==========================
// Definição da imagem
// ==========================

var nomeArquivoImagem = "pessoas.png";
var caminhoImagem = Path.Combine(
    Directory.GetCurrentDirectory(),
    "Imagens",
    nomeArquivoImagem
);

if (!File.Exists(caminhoImagem))
{
    Console.WriteLine($"Imagem não encontrada: {caminhoImagem}");
    return;
}

// ==========================
// Exibe a imagem primeiro
// ==========================

Console.WriteLine("A imagem a ser analisada será exibida agora.");
Console.WriteLine("Observe a imagem com atenção.");
Console.WriteLine("Pressione qualquer tecla para abrir a imagem...");
Console.ReadKey();

Process.Start(new ProcessStartInfo
{
    FileName = Path.GetFullPath(caminhoImagem),
    UseShellExecute = true
});

// ==========================
// Coleta das perguntas
// ==========================

string systemPrompt = """
    Você é um assistente especializado em análise de imagens.
    Responda TODAS as perguntas feitas pelo usuário.
    Numere as respostas de acordo com as perguntas.
    Seja direto e objetivo.
    """;

Console.WriteLine();
Console.WriteLine("Agora digite 3 perguntas sobre a imagem:");
Console.WriteLine();

var perguntas = new List<string>();

for (int i = 1; i <= 3; i++)
{
    Console.Write($"Pergunta {i}: ");
    var pergunta = Console.ReadLine();

    if (!string.IsNullOrWhiteSpace(pergunta))
        perguntas.Add(pergunta);
}

// fallback caso o usuário não digite nada
if (perguntas.Count == 0)
{
    perguntas.Add("Descreva a imagem em detalhes.");
}

// ==========================
// Montagem das mensagens
// ==========================

var mensagens = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, systemPrompt)
};

foreach (var pergunta in perguntas)
{
    mensagens.Add(new ChatMessage(ChatRole.User, pergunta));
}

// adiciona a imagem como conteúdo multimodal
AIContent imagemContent = new DataContent(
    File.ReadAllBytes(caminhoImagem), "image/png");

mensagens.Add(
    new ChatMessage(ChatRole.User, new[] { imagemContent })
);

// ==========================
// Envio para o modelo
// ==========================

Console.WriteLine();
Console.WriteLine("Pressione qualquer tecla para iniciar a análise da imagem...");
Console.ReadKey();

Console.WriteLine("\nProcessando...\n");

var response = await client.GetResponseAsync(mensagens);

// ==========================
// Exibição do resultado
// ==========================

Console.WriteLine("Perguntas feitas ao modelo:");
int contador = 1;
foreach (var pergunta in perguntas)
{
    Console.WriteLine($"  {contador++}. {pergunta}");
}

Console.WriteLine($"\nImagem analisada: {nomeArquivoImagem}");
Console.WriteLine("\nResposta do modelo:\n");

Console.WriteLine(response.Messages[0]);

Console.WriteLine("\nPressione qualquer tecla para encerrar...");
Console.ReadKey();
