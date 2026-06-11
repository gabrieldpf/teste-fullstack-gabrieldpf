
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        private readonly VeiculoHistoricoService _historico;
        public VeiculosController(AppDbContext db, PlacaService placa, VeiculoHistoricoService historico)
        {
            _db = db;
            _placa = placa;
            _historico = historico;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? clienteId = null)
        {
            var q = _db.Veiculos.AsQueryable();
            if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId.Value);
            var list = await q.OrderBy(v => v.Placa).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VeiculoCreateDto dto)
        {
            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest(new { mensagem = "Placa inválida. Use o formato Mercosul (ex.: ABC1D23)." });
            if (await _db.Veiculos.AnyAsync(v => v.Placa == placa)) return Conflict(new { mensagem = "Placa já cadastrada." });
            if (!await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
                return BadRequest(new { mensagem = "Cliente informado não existe." });

            var v = new Veiculo { Placa = placa, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId };
            _db.Veiculos.Add(v);
            await _db.SaveChangesAsync();
            await _historico.RegistrarInicioAsync(v);
            return CreatedAtAction(nameof(GetById), new { id = v.Id }, v);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var v = await _db.Veiculos.FindAsync(id);
            return v == null ? NotFound() : Ok(v);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] VeiculoUpdateDto dto)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound();

            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest(new { mensagem = "Placa inválida. Use o formato Mercosul (ex.: ABC1D23)." });
            if (await _db.Veiculos.AnyAsync(x => x.Placa == placa && x.Id != id))
                return Conflict(new { mensagem = "Placa já cadastrada em outro veículo." });
            if (!await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
                return BadRequest(new { mensagem = "Cliente informado não existe." });

            v.Placa = placa;
            v.Modelo = dto.Modelo;
            v.Ano = dto.Ano;

            if (v.ClienteId != dto.ClienteId)
                await _historico.RegistrarTrocaAsync(v, dto.ClienteId, DateTime.UtcNow);
            else
                await _db.SaveChangesAsync();

            return Ok(v);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound();
            _db.Veiculos.Remove(v);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
