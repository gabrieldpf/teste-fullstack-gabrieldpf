
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
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ClientesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pagina = 1, [FromQuery] int tamanho = 10, [FromQuery] string? filtro = null, [FromQuery] string mensalista = "all")
        {
            var q = _db.Clientes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtro))
                q = q.Where(c => c.Nome.Contains(filtro));
            if (mensalista == "true") q = q.Where(c => c.Mensalista);
            if (mensalista == "false") q = q.Where(c => !c.Mensalista);

            var total = await q.CountAsync();
            var itens = await q
                .OrderBy(c => c.Nome)
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .ToListAsync();
            return Ok(new { total, itens });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
        {
            var telefone = TelefoneService.Normalizar(dto.Telefone);
            if (await ExisteDuplicadoAsync(dto.Nome, telefone))
                return Conflict(new { mensagem = "Já existe um cliente com este Nome e Telefone." });

            var c = new Cliente
            {
                Nome = dto.Nome.Trim(),
                Telefone = telefone,
                Endereco = dto.Endereco?.Trim(),
                Mensalista = dto.Mensalista,
                ValorMensalidade = dto.ValorMensalidade,
            };
            _db.Clientes.Add(c);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var c = await _db.Clientes.Include(x => x.Veiculos).FirstOrDefaultAsync(x => x.Id == id);
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ClienteUpdateDto dto)
        {
            var c = await _db.Clientes.FindAsync(id);
            if (c == null) return NotFound();

            var telefone = TelefoneService.Normalizar(dto.Telefone);
            if (await ExisteDuplicadoAsync(dto.Nome, telefone, id))
                return Conflict(new { mensagem = "Já existe outro cliente com este Nome e Telefone." });

            c.Nome = dto.Nome.Trim();
            c.Telefone = telefone;
            c.Endereco = dto.Endereco?.Trim();
            c.Mensalista = dto.Mensalista;
            c.ValorMensalidade = dto.ValorMensalidade;
            await _db.SaveChangesAsync();
            return Ok(c);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var c = await _db.Clientes.FindAsync(id);
            if (c == null) return NotFound();
            var temVeiculos = await _db.Veiculos.AnyAsync(v => v.ClienteId == id);
            if (temVeiculos) return BadRequest(new { mensagem = "Cliente possui veículos associados. Transfira ou remova antes." });
            _db.Clientes.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private Task<bool> ExisteDuplicadoAsync(string nome, string? telefone, Guid? ignorarId = null)
        {
            var q = _db.Clientes.Where(c => c.Nome == nome.Trim() && c.Telefone == telefone);
            if (ignorarId.HasValue) q = q.Where(c => c.Id != ignorarId.Value);
            return q.AnyAsync();
        }
    }
}
