
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;

namespace Parking.Api.Services
{
    public class VeiculoHistoricoService
    {
        private readonly AppDbContext _db;
        public VeiculoHistoricoService(AppDbContext db) => _db = db;

        public async Task RegistrarInicioAsync(Veiculo veiculo, CancellationToken ct = default)
        {
            _db.VeiculoHistoricos.Add(new VeiculoClienteHistorico
            {
                VeiculoId = veiculo.Id,
                ClienteId = veiculo.ClienteId,
                DataInicio = veiculo.DataInclusao
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task RegistrarTrocaAsync(Veiculo veiculo, Guid novoClienteId, DateTime dataTroca, CancellationToken ct = default)
        {
            if (veiculo.ClienteId == novoClienteId) return;

            var atual = await _db.VeiculoHistoricos
                .Where(h => h.VeiculoId == veiculo.Id && h.DataFim == null)
                .OrderByDescending(h => h.DataInicio)
                .FirstOrDefaultAsync(ct);

            if (atual != null)
                atual.DataFim = dataTroca;

            _db.VeiculoHistoricos.Add(new VeiculoClienteHistorico
            {
                VeiculoId = veiculo.Id,
                ClienteId = novoClienteId,
                DataInicio = dataTroca
            });

            veiculo.ClienteId = novoClienteId;
            veiculo.DataInclusao = dataTroca;
            await _db.SaveChangesAsync(ct);
        }
    }
}
