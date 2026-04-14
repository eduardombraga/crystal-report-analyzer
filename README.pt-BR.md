# Crystal Report Analyzer

[Read in English](README.md)

Ferramenta WPF para análise de arquivos SAP Crystal Reports (`.rpt`). Extrai tabelas, campos, parâmetros, fórmulas, grupos, seções e sub-relatórios; pontua a complexidade do relatório; classifica objetos de banco de dados; e gera stubs de código C# para migração.

## Funcionalidades

- Abrir ou arrastar qualquer arquivo `.rpt`
- Pontuação de complexidade com classificação visual (Simples / Médio / Complexo / Muito Complexo)
- Análise de dependências: classifica objetos do banco como Tabela, View, Stored Procedure ou Function
- Geração de código C# para migração de relatórios para HTML/Razor
- Exportação da análise completa para JSON ou dos stubs gerados para um arquivo `.cs`

## Pré-requisitos

### 1. SAP Crystal Reports Runtime (obrigatório)

As DLLs do Crystal Reports são **proprietárias e não redistribuíveis**, portanto não estão incluídas neste repositório. É necessário instalá-las separadamente:

1. Baixe o **SAP Crystal Reports for Visual Studio** (gratuito para desenvolvimento):
   [https://www.sap.com/products/crystal-reports/downloads.html](https://www.sap.com/products/crystal-reports/downloads.html)
2. Execute o instalador.
3. Após a instalação, copie as DLLs abaixo do diretório de instalação para `CrystalReportAnalyzer/libs/`:

   | DLL | Localização típica |
   |-----|-------------------|
   | `CrystalDecisions.CrystalReports.Engine.dll` | `C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\` |
   | `CrystalDecisions.Shared.dll` | mesma pasta |
   | `CrystalDecisions.ReportSource.dll` | mesma pasta |

   Copie também todos os outros arquivos `CrystalDecisions.*.dll` — eles são carregados dinamicamente em tempo de execução.

> **Atenção:** O Crystal Reports é um runtime 32-bit. Se ocorrerem erros de carregamento, descomente `<PlatformTarget>x86</PlatformTarget>` no `.csproj`.

### 2. .NET SDK

- [.NET SDK 4.8+](https://dotnet.microsoft.com/download) (o projeto usa `net48`)

## Build e execução

```bash
# Build
dotnet build CrystalReportAnalyzer.sln

# Executar
dotnet run --project CrystalReportAnalyzer/CrystalReportAnalyzer.csproj
```

## Arquitetura

O projeto é organizado em um pipeline: **Extractors → Service → Analyzers → UI / Exporters**.

- **Extractors** — uma classe por responsabilidade (`DatabaseExtractor`, `FormulaExtractor`, `ParameterExtractor`, `GroupExtractor`, `SectionExtractor`, `SubreportExtractor`), cada uma recebendo um `ReportDocument` e retornando objetos de modelo tipados.
- **ReportAnalysisService** — orquestra todos os extractors e alimenta o resultado em dois analyzers:
  - `ComplexityAnalyzer` — pontua o relatório e o classifica como Simples / Médio / Complexo / Muito Complexo.
  - `DependencyAnalyzer` — classifica cada objeto do banco como Tabela, View, Stored Procedure ou Function com base em convenções de nomenclatura.
- **CSharpCodeGenerator** — gera quatro blocos de código C# (DTOs, classe de parâmetros, interface de serviço, regras de negócio) para auxiliar a migração do Crystal Reports.
- **Exporters** — `JsonExporter` serializa o modelo completo; `CSharpExporter` salva os stubs gerados em um arquivo `.cs`.
- **MainWindow** — code-behind WPF (sem MVVM) que controla a árvore de itens, o painel de complexidade, o painel de dependências e os botões de exportação.

Para detalhes completos — pesos de pontuação, regras de inferência de tipo e fluxo de dados — consulte o [CLAUDE.md](CLAUDE.md).

## Licença

MIT — veja [LICENSE](LICENSE).

O Crystal Reports runtime é software proprietário da SAP SE e está sujeito aos termos de licença da SAP. Não está incluído neste repositório.
