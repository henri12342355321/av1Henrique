using System;
using System.Linq;
using System.Windows.Forms;
using EstacionamentoApp.Models;
using EstacionamentoApp.Data;
using System.IO;

namespace EstacionamentoApp
{
    public class MainForm : Form
    {
        private readonly DataAccess _db;
        private readonly string _dbPath;
        private TabControl tabs;
        // controls for vehicles
        private TextBox txtPlaca, txtModelo, txtCor, txtProprietario;
        private Button btnAddVeiculo;
        private DataGridView dgvVeiculos;

        // controls for vagas
        private NumericUpDown numVaga;
        private ComboBox cbTipoVaga;
        private Button btnAddVaga;
        private DataGridView dgvVagas;

        // entrada/saida
        private ComboBox cbVeiculosEntrada, cbVagasEntrada;
        private Button btnRegistrarEntrada;
        private DataGridView dgvAtivos;
        private Button btnRegistrarSaida;

        // histórico
        private DataGridView dgvHistorico;

        public MainForm()
        {
            Text = "Sistema de Estacionamento";
            Width = 900;
            Height = 600;

            _dbPath = Path.Combine(AppContext.BaseDirectory, "estacionamento.db");
            _db = new DataAccess(_dbPath);

            InitializeComponents();
            LoadAll();
        }

        private void InitializeComponents()
        {
            tabs = new TabControl { Dock = DockStyle.Fill };

            var tabVeiculos = new TabPage("Veículos");
            var tabVagas = new TabPage("Vagas");
            var tabMov = new TabPage("Entrada / Saída");
            var tabList = new TabPage("Listagens");

            // Veiculos tab
            var pnlV = new Panel { Dock = DockStyle.Top, Height = 120 };
            txtPlaca = new TextBox { Left = 10, Top = 10, Width = 120, PlaceholderText = "Placa" };
            txtModelo = new TextBox { Left = 140, Top = 10, Width = 180, PlaceholderText = "Modelo" };
            txtCor = new TextBox { Left = 330, Top = 10, Width = 120, PlaceholderText = "Cor" };
            txtProprietario = new TextBox { Left = 460, Top = 10, Width = 250, PlaceholderText = "Proprietário" };
            btnAddVeiculo = new Button { Left = 720, Top = 8, Width = 120, Text = "Salvar Veículo" };
            btnAddVeiculo.Click += BtnAddVeiculo_Click;
            pnlV.Controls.AddRange(new Control[]{ txtPlaca, txtModelo, txtCor, txtProprietario, btnAddVeiculo });

            dgvVeiculos = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            tabVeiculos.Controls.Add(dgvVeiculos);
            tabVeiculos.Controls.Add(pnlV);

            // Vagas tab
            var pnlG = new Panel { Dock = DockStyle.Top, Height = 80 };
            numVaga = new NumericUpDown { Left = 10, Top = 10, Width = 80, Minimum = 1, Maximum = 1000, Value = 1 };
            cbTipoVaga = new ComboBox { Left = 100, Top = 10, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cbTipoVaga.Items.AddRange(new[] { "Carro", "Moto" });
            cbTipoVaga.SelectedIndex = 0;
            btnAddVaga = new Button { Left = 230, Top = 8, Width = 120, Text = "Salvar Vaga" };
            btnAddVaga.Click += BtnAddVaga_Click;
            pnlG.Controls.AddRange(new Control[]{ numVaga, cbTipoVaga, btnAddVaga });

            dgvVagas = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            tabVagas.Controls.Add(dgvVagas);
            tabVagas.Controls.Add(pnlG);

            // Entrada/saída tab
            var pnlE = new Panel { Dock = DockStyle.Top, Height = 100 };
            cbVeiculosEntrada = new ComboBox { Left = 10, Top = 10, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbVagasEntrada = new ComboBox { Left = 320, Top = 10, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            btnRegistrarEntrada = new Button { Left = 530, Top = 8, Width = 150, Text = "Registrar Entrada" };
            btnRegistrarEntrada.Click += BtnRegistrarEntrada_Click;
            btnRegistrarSaida = new Button { Left = 690, Top = 8, Width = 150, Text = "Registrar Saída" };
            btnRegistrarSaida.Click += BtnRegistrarSaida_Click;
            pnlE.Controls.AddRange(new Control[]{ cbVeiculosEntrada, cbVagasEntrada, btnRegistrarEntrada, btnRegistrarSaida });

            dgvAtivos = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            tabMov.Controls.Add(dgvAtivos);
            tabMov.Controls.Add(pnlE);

            // Listagens tab (historico)
            dgvHistorico = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            tabList.Controls.Add(dgvHistorico);

            tabs.TabPages.AddRange(new[] { tabVeiculos, tabVagas, tabMov, tabList });
            Controls.Add(tabs);
        }

        private void BtnAddVeiculo_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtPlaca.Text)) { MessageBox.Show("Placa é obrigatória."); return; }
                var v = new Veiculo { Placa = txtPlaca.Text.Trim().ToUpper(), Modelo = txtModelo.Text, Cor = txtCor.Text, Proprietario = txtProprietario.Text };
                _db.AddVeiculo(v);
                ClearVehicleInputs();
                LoadAll();
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private void BtnAddVaga_Click(object? sender, EventArgs e)
        {
            try
            {
                var v = new Vaga { Numero = (int)numVaga.Value, Tipo = cbTipoVaga.SelectedItem?.ToString() ?? "Carro", Status = "LIVRE" };
                _db.AddVaga(v);
                LoadAll();
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private void BtnRegistrarEntrada_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbVeiculosEntrada.SelectedItem is not Veiculo ve) { MessageBox.Show("Selecione um veículo."); return; }
                if (cbVagasEntrada.SelectedItem is not Vaga vg) { MessageBox.Show("Selecione uma vaga livre."); return; }
                _db.RegisterEntrada(ve.Id, vg.Id);
                LoadAll();
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private void BtnRegistrarSaida_Click(object? sender, EventArgs e)
        {
            try
            {
                if (dgvAtivos.CurrentRow == null) { MessageBox.Show("Selecione um movimento ativo na tabela abaixo."); return; }
                var tuple = dgvAtivos.CurrentRow.DataBoundItem as System.Tuple<int, string>;
                // we bound as an anonymous projection; instead read the first cell which contains movimento id (hidden)
                int movId = Convert.ToInt32(dgvAtivos.CurrentRow.Cells["MovimentoId"].Value);
                _db.RegisterSaida(movId);
                LoadAll();
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
        }

        private void LoadAll()
        {
            // veiculos
            var veiculos = _db.GetVeiculos();
            dgvVeiculos.DataSource = veiculos.Select(v => new { v.Id, v.Placa, v.Modelo, v.Cor, v.Proprietario }).ToList();
            // vagas
            var vagas = _db.GetVagas();
            dgvVagas.DataSource = vagas.Select(g => new { g.Id, g.Numero, g.Tipo, g.Status }).ToList();

            // preencher comboboxes
            cbVeiculosEntrada.Items.Clear();
            foreach (var v in veiculos) cbVeiculosEntrada.Items.Add(v);
            cbVagasEntrada.Items.Clear();
            var vagasLivres = _db.GetVagasLivres();
            foreach (var g in vagasLivres) cbVagasEntrada.Items.Add(g);

            // ativos
            var ativos = _db.GetAtivos();
            // bind a table with MovimentoId, Placa, Modelo, VagaNumero, Entrada
            var ativosBind = ativos.Select(t => new {
                MovimentoId = t.Item1.Id,
                Placa = t.Item2.Placa,
                Modelo = t.Item2.Modelo,
                Vaga = t.Item3.Numero,
                Entrada = t.Item1.DataEntrada
            }).ToList();
            dgvAtivos.DataSource = ativosBind;
            if (dgvAtivos.Columns["MovimentoId"] != null) dgvAtivos.Columns["MovimentoId"].Visible = false;

            // historico
            var hist = _db.GetHistorico();
            var histBind = hist.Select(t => new {
                MovimentoId = t.Item1.Id,
                Placa = t.Item2.Placa,
                Modelo = t.Item2.Modelo,
                Vaga = t.Item3.Numero,
                Entrada = t.Item1.DataEntrada,
                Saida = t.Item1.DataSaida
            }).ToList();
            dgvHistorico.DataSource = histBind;
            if (dgvHistorico.Columns["MovimentoId"] != null) dgvHistorico.Columns["MovimentoId"].Visible = false;
        }

        private void ClearVehicleInputs()
        {
            txtPlaca.Text = "";
            txtModelo.Text = "";
            txtCor.Text = "";
            txtProprietario.Text = "";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _db.Dispose();
        }
    }
}
