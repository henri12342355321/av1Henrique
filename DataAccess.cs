using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using EstacionamentoApp.Models;

namespace EstacionamentoApp.Data
{
    public class DataAccess : IDisposable
    {
        private readonly SqliteConnection _conn;
        public DataAccess(string dbPath)
        {
            _conn = new SqliteConnection($"Data Source={dbPath}");
            _conn.Open();
            Init();
        }

        private void Init()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Veiculo (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    placa TEXT UNIQUE NOT NULL,
    modelo TEXT,
    cor TEXT,
    proprietario TEXT
);
CREATE TABLE IF NOT EXISTS Vaga (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    numero INTEGER UNIQUE NOT NULL,
    tipo TEXT,
    status TEXT DEFAULT 'LIVRE'
);
CREATE TABLE IF NOT EXISTS Movimento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    veiculoId INTEGER,
    vagaId INTEGER,
    dataEntrada TEXT DEFAULT (datetime('now')),
    dataSaida TEXT NULL,
    FOREIGN KEY(veiculoId) REFERENCES Veiculo(id),
    FOREIGN KEY(vagaId) REFERENCES Vaga(id)
);
";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _conn.Dispose();

        // Veiculo
        public void AddVeiculo(Veiculo v)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Veiculo (placa, modelo, cor, proprietario) VALUES (@p,@m,@c,@pr)";
            cmd.Parameters.AddWithValue("@p", v.Placa);
            cmd.Parameters.AddWithValue("@m", v.Modelo ?? "");
            cmd.Parameters.AddWithValue("@c", v.Cor ?? "");
            cmd.Parameters.AddWithValue("@pr", v.Proprietario ?? "");
            cmd.ExecuteNonQuery();
        }

        public List<Veiculo> GetVeiculos()
        {
            var list = new List<Veiculo>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT id, placa, modelo, cor, proprietario FROM Veiculo ORDER BY placa";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Veiculo{
                    Id = r.GetInt32(0),
                    Placa = r.GetString(1),
                    Modelo = r.GetString(2),
                    Cor = r.GetString(3),
                    Proprietario = r.GetString(4)
                });
            }
            return list;
        }

        // Vaga
        public void AddVaga(Vaga v)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Vaga (numero, tipo, status) VALUES (@n,@t,@s)";
            cmd.Parameters.AddWithValue("@n", v.Numero);
            cmd.Parameters.AddWithValue("@t", v.Tipo ?? "Carro");
            cmd.Parameters.AddWithValue("@s", v.Status ?? "LIVRE");
            cmd.ExecuteNonQuery();
        }

        public List<Vaga> GetVagas()
        {
            var list = new List<Vaga>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT id, numero, tipo, status FROM Vaga ORDER BY numero";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Vaga{
                    Id = r.GetInt32(0),
                    Numero = r.GetInt32(1),
                    Tipo = r.GetString(2),
                    Status = r.GetString(3)
                });
            }
            return list;
        }

        public List<Vaga> GetVagasLivres()
        {
            var list = new List<Vaga>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT id, numero, tipo, status FROM Vaga WHERE status='LIVRE' ORDER BY numero";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Vaga{
                    Id = r.GetInt32(0),
                    Numero = r.GetInt32(1),
                    Tipo = r.GetString(2),
                    Status = r.GetString(3)
                });
            }
            return list;
        }

        public void SetVagaStatus(int vagaId, string status)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE Vaga SET status=@s WHERE id=@id";
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@id", vagaId);
            cmd.ExecuteNonQuery();
        }

        // Movimento
        public void RegisterEntrada(int veiculoId, int vagaId)
        {
            using var tran = _conn.BeginTransaction();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Movimento (veiculoId, vagaId) VALUES (@v,@g)";
            cmd.Parameters.AddWithValue("@v", veiculoId);
            cmd.Parameters.AddWithValue("@g", vagaId);
            cmd.ExecuteNonQuery();

            using var cmd2 = _conn.CreateCommand();
            cmd2.CommandText = "UPDATE Vaga SET status='OCUPADA' WHERE id=@id";
            cmd2.Parameters.AddWithValue("@id", vagaId);
            cmd2.ExecuteNonQuery();
            tran.Commit();
        }

        public void RegisterSaida(int movimentoId)
        {
            // set dataSaida and free the vaga
            // get vagaId from movimento
            int vagaId = -1;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT vagaId FROM Movimento WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", movimentoId);
                using var r = cmd.ExecuteReader();
                if (r.Read()) vagaId = r.GetInt32(0);
            }

            using var tran = _conn.BeginTransaction();
            using var cmd2 = _conn.CreateCommand();
            cmd2.CommandText = "UPDATE Movimento SET dataSaida = datetime('now') WHERE id=@id";
            cmd2.Parameters.AddWithValue("@id", movimentoId);
            cmd2.ExecuteNonQuery();

            if (vagaId != -1)
            {
                using var cmd3 = _conn.CreateCommand();
                cmd3.CommandText = "UPDATE Vaga SET status='LIVRE' WHERE id=@id";
                cmd3.Parameters.AddWithValue("@id", vagaId);
                cmd3.ExecuteNonQuery();
            }
            tran.Commit();
        }

        public List<(Movimento, Veiculo, Vaga)> GetAtivos()
        {
            var list = new List<(Movimento, Veiculo, Vaga)>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
SELECT m.id, m.veiculoId, m.vagaId, m.dataEntrada,
       v.id, v.placa, v.modelo, v.cor, v.proprietario,
       g.id, g.numero, g.tipo, g.status
FROM Movimento m
JOIN Veiculo v ON v.id = m.veiculoId
JOIN Vaga g ON g.id = m.vagaId
WHERE m.dataSaida IS NULL
ORDER BY m.dataEntrada
";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var m = new Movimento {
                    Id = r.GetInt32(0),
                    VeiculoId = r.GetInt32(1),
                    VagaId = r.GetInt32(2),
                    DataEntrada = DateTime.Parse(r.GetString(3))
                };
                var v = new Veiculo {
                    Id = r.GetInt32(4),
                    Placa = r.GetString(5),
                    Modelo = r.GetString(6),
                    Cor = r.GetString(7),
                    Proprietario = r.GetString(8)
                };
                var g = new Vaga {
                    Id = r.GetInt32(9),
                    Numero = r.GetInt32(10),
                    Tipo = r.GetString(11),
                    Status = r.GetString(12)
                };
                list.Add((m,v,g));
            }
            return list;
        }

        public List<(Movimento, Veiculo, Vaga)> GetHistorico()
        {
            var list = new List<(Movimento, Veiculo, Vaga)>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
SELECT m.id, m.veiculoId, m.vagaId, m.dataEntrada, m.dataSaida,
       v.id, v.placa, v.modelo, v.cor, v.proprietario,
       g.id, g.numero, g.tipo, g.status
FROM Movimento m
JOIN Veiculo v ON v.id = m.veiculoId
JOIN Vaga g ON g.id = m.vagaId
ORDER BY m.dataEntrada DESC
";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var m = new Movimento {
                    Id = r.GetInt32(0),
                    VeiculoId = r.GetInt32(1),
                    VagaId = r.GetInt32(2),
                    DataEntrada = DateTime.Parse(r.GetString(3)),
                    DataSaida = r.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(r.GetString(4))
                };
                var v = new Veiculo {
                    Id = r.GetInt32(5),
                    Placa = r.GetString(6),
                    Modelo = r.GetString(7),
                    Cor = r.GetString(8),
                    Proprietario = r.GetString(9)
                };
                var g = new Vaga {
                    Id = r.GetInt32(10),
                    Numero = r.GetInt32(11),
                    Tipo = r.GetString(12),
                    Status = r.GetString(13)
                };
                list.Add((m,v,g));
            }
            return list;
        }
    }
}
