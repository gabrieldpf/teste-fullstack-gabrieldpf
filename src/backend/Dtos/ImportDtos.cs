
namespace Parking.Api.Dtos
{
    public record CsvErroDto(int Linha, string Motivo, string? Conteudo);

    public record CsvImportResultDto(
        int Processados,
        int Inseridos,
        int Erros,
        IReadOnlyList<CsvErroDto> Detalhes
    );
}
