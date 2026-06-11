
namespace Parking.Api.Models
{
    public class VeiculoClienteHistorico
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VeiculoId { get; set; }
        public Guid ClienteId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public Veiculo? Veiculo { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
