using System;

namespace EstacionamentoApp.Models
{
    public class Veiculo
    {
        public int Id { get; set; }
        public string Placa { get; set; }
        public string Modelo { get; set; }
        public string Cor { get; set; }
        public string Proprietario { get; set; }
        public override string ToString() => $"{Placa} - {Modelo}";
    }

    public class Vaga
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Tipo { get; set; } // Carro/Moto
        public string Status { get; set; } // LIVRE / OCUPADA
        public override string ToString() => $"Vaga {Numero} ({Tipo}) - {Status}";
    }

    public class Movimento
    {
        public int Id { get; set; }
        public int VeiculoId { get; set; }
        public int VagaId { get; set; }
        public DateTime DataEntrada { get; set; }
        public DateTime? DataSaida { get; set; }
    }
}
