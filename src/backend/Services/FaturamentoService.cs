
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;

namespace Parking.Api.Services
{
    public class FaturamentoService
    {
        private readonly AppDbContext _db;
        public FaturamentoService(AppDbContext db) => _db = db;

        public async Task<List<Fatura>> GerarAsync(string competencia, CancellationToken ct = default)
        {
            var part = competencia.Split('-');
            if (part.Length != 2 || !int.TryParse(part[0], out var ano) || !int.TryParse(part[1], out var mes) || mes < 1 || mes > 12)
                throw new ArgumentException("Competência inválida. Use o formato yyyy-MM.");

            var diasNoMes = DateTime.DaysInMonth(ano, mes);
            var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fimMes = new DateTime(ano, mes, diasNoMes, 23, 59, 59, DateTimeKind.Utc);

            var mensalistas = await _db.Clientes
                .Where(c => c.Mensalista)
                .AsNoTracking()
                .ToListAsync(ct);

            var criadas = new List<Fatura>();

            foreach (var cli in mensalistas)
            {
                var existente = await _db.Faturas
                    .FirstOrDefaultAsync(f => f.ClienteId == cli.Id && f.Competencia == competencia, ct);
                if (existente != null) continue;

                var historicos = await _db.VeiculoHistoricos
                    .Where(h => h.ClienteId == cli.Id)
                    .Where(h => h.DataInicio <= fimMes && (h.DataFim == null || h.DataFim >= inicioMes))
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (historicos.Count == 0) continue;

                var veiculoIds = historicos.Select(h => h.VeiculoId).Distinct().ToList();
                var diasCobrados = CalcularDiasCobrados(historicos, inicioMes, fimMes);
                var valorBase = cli.ValorMensalidade ?? 0m;
                var valor = Math.Round(valorBase * diasCobrados / diasNoMes, 2);

                var fat = new Fatura
                {
                    Competencia = competencia,
                    ClienteId = cli.Id,
                    Valor = valor,
                    Observacao = diasCobrados < diasNoMes
                        ? $"Faturamento proporcional: {diasCobrados}/{diasNoMes} dias"
                        : null
                };

                foreach (var id in veiculoIds)
                    fat.Veiculos.Add(new FaturaVeiculo { FaturaId = fat.Id, VeiculoId = id });

                _db.Faturas.Add(fat);
                criadas.Add(fat);
            }

            await _db.SaveChangesAsync(ct);
            return criadas;
        }

        internal static int CalcularDiasCobrados(
            IEnumerable<VeiculoClienteHistorico> historicos,
            DateTime inicioMes,
            DateTime fimMes)
        {
            var dias = new HashSet<int>();

            foreach (var h in historicos)
            {
                var inicio = h.DataInicio < inicioMes ? inicioMes : h.DataInicio.Date;
                var fim = (h.DataFim ?? fimMes);
                if (fim > fimMes) fim = fimMes;

                for (var d = inicio; d.Date <= fim.Date; d = d.AddDays(1))
                    dias.Add(d.Day);
            }

            return dias.Count;
        }
    }
}
