
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;
using Parking.Api.Services;
using System.Text;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        private readonly VeiculoHistoricoService _historico;

        private static readonly string[] ColunasEsperadas =
        {
            "placa", "modelo", "ano", "cliente_identificador", "cliente_nome",
            "cliente_telefone", "cliente_endereco", "mensalista", "valor_mensalidade"
        };

        public ImportController(AppDbContext db, PlacaService placa, VeiculoHistoricoService historico)
        {
            _db = db;
            _placa = placa;
            _historico = historico;
        }

        [HttpPost("csv")]
        public async Task<IActionResult> ImportCsv(CancellationToken ct)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { mensagem = "Envie um arquivo CSV no campo 'file'." });

            var file = Request.Form.Files[0];
            using var s = file.OpenReadStream();
            using var r = new StreamReader(s, Encoding.UTF8);

            int linha = 0, processados = 0, inseridos = 0;
            var erros = new List<CsvErroDto>();

            var header = await r.ReadLineAsync(ct);
            linha = 1;
            if (header == null)
                return BadRequest(new { mensagem = "Arquivo CSV vazio." });

            var headerCols = header.Split(',').Select(c => c.Trim().ToLowerInvariant()).ToArray();
            if (!ColunasEsperadas.SequenceEqual(headerCols))
            {
                return BadRequest(new
                {
                    mensagem = "Cabeçalho inválido. Esperado: " + string.Join(",", ColunasEsperadas)
                });
            }

            while (!r.EndOfStream)
            {
                linha++;
                var raw = await r.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(raw)) continue;
                processados++;

                var cols = raw.Split(',');
                if (cols.Length < 9)
                {
                    erros.Add(new CsvErroDto(linha, "Linha incompleta: são necessárias 9 colunas.", raw));
                    continue;
                }

                try
                {
                    var placa = _placa.Sanitizar(cols[0]);
                    var modelo = cols[1].Trim();
                    if (!int.TryParse(cols[2].Trim(), out var anoParsed))
                    {
                        erros.Add(new CsvErroDto(linha, "Ano inválido. Informe um número inteiro.", raw));
                        continue;
                    }
                    var cliNome = cols[4].Trim();
                    var cliTel = TelefoneService.Normalizar(cols[5]);
                    var cliEnd = cols[6].Trim();
                    if (!bool.TryParse(cols[7].Trim(), out var mensalista))
                    {
                        erros.Add(new CsvErroDto(linha, "Campo 'mensalista' inválido. Use true ou false.", raw));
                        continue;
                    }
                    decimal? valorMens = decimal.TryParse(cols[8].Trim(), out var vm) ? vm : null;

                    if (string.IsNullOrWhiteSpace(cliNome))
                    {
                        erros.Add(new CsvErroDto(linha, "Nome do cliente é obrigatório.", raw));
                        continue;
                    }

                    if (!_placa.EhValida(placa))
                    {
                        erros.Add(new CsvErroDto(linha, $"Placa '{cols[0].Trim()}' inválida. Use formato Mercosul (ex.: ABC1D23).", raw));
                        continue;
                    }

                    if (await _db.Veiculos.AnyAsync(v => v.Placa == placa, ct))
                    {
                        erros.Add(new CsvErroDto(linha, $"Placa '{placa}' já está cadastrada no sistema.", raw));
                        continue;
                    }

                    var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Nome == cliNome && c.Telefone == cliTel, ct);
                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            Nome = cliNome,
                            Telefone = cliTel,
                            Endereco = cliEnd,
                            Mensalista = mensalista,
                            ValorMensalidade = valorMens
                        };
                        _db.Clientes.Add(cliente);
                        await _db.SaveChangesAsync(ct);
                    }

                    var v = new Veiculo
                    {
                        Placa = placa,
                        Modelo = modelo,
                        Ano = anoParsed,
                        ClienteId = cliente.Id
                    };
                    _db.Veiculos.Add(v);
                    await _db.SaveChangesAsync(ct);
                    await _historico.RegistrarInicioAsync(v, ct);
                    inseridos++;
                }
                catch (DbUpdateException)
                {
                    erros.Add(new CsvErroDto(linha, "Erro de integridade ao salvar (possível duplicidade de cliente ou placa).", raw));
                }
                catch (Exception ex)
                {
                    erros.Add(new CsvErroDto(linha, ex.Message, raw));
                }
            }

            return Ok(new CsvImportResultDto(processados, inseridos, erros.Count, erros));
        }
    }
}
