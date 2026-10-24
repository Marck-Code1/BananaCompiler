namespace BananaCompiler
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            txtCode = new ScintillaNET.Scintilla();
            txtTokens = new ScintillaNET.Scintilla();
            btnCargar = new Button();
            btnGuardarPrograma = new Button();
            btnGuardarArchivo = new Button();
            dgSimbolos = new DataGridView();
            ID = new DataGridViewTextBoxColumn();
            Nombre = new DataGridViewTextBoxColumn();
            TipoDato = new DataGridViewTextBoxColumn();
            Valor = new DataGridViewTextBoxColumn();
            Ambito = new DataGridViewTextBoxColumn();
            Direccion = new DataGridViewTextBoxColumn();
            Tamano = new DataGridViewTextBoxColumn();
            dgErrores = new DataGridView();
            Linea = new DataGridViewTextBoxColumn();
            Error = new DataGridViewTextBoxColumn();
            dgErroresSintacticos = new DataGridView();
            LineaSintactica = new DataGridViewTextBoxColumn();
            ErrorSintactico = new DataGridViewTextBoxColumn();
            lblLenguaje = new Label();
            pictureBox1 = new PictureBox();
            richTextBox1 = new RichTextBox();
            lblCodigo = new Label();
            lblTokens = new Label();
            lblTabla = new Label();
            lblErrores = new Label();
            lblErroresSintacticos = new Label();
            lblSubtitle = new Label();
            label1 = new Label();
            txtReporte = new RichTextBox();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)dgSimbolos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgErrores).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgErroresSintacticos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // txtCode
            // 
            txtCode.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            txtCode.Font = new Font("Consolas", 9.75F);
            txtCode.LexerName = null;
            txtCode.Location = new Point(46, 267);
            txtCode.Name = "txtCode";
            txtCode.ScrollWidth = 74;
            txtCode.Size = new Size(641, 372);
            txtCode.TabIndex = 0;
            txtCode.Text = "start\r\n\r\nend";
            txtCode.TextChanged += txtCode_TextChanged;
            // 
            // txtTokens
            // 
            txtTokens.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            txtTokens.Font = new Font("Consolas", 9.75F);
            txtTokens.LexerName = null;
            txtTokens.Location = new Point(813, 267);
            txtTokens.Name = "txtTokens";
            txtTokens.ScrollWidth = 74;
            txtTokens.Size = new Size(641, 372);
            txtTokens.TabIndex = 1;
            txtTokens.TextChanged += txtTokens_TextChanged_Lines;
            // 
            // btnCargar
            // 
            btnCargar.Location = new Point(46, 648);
            btnCargar.Name = "btnCargar";
            btnCargar.Size = new Size(111, 38);
            btnCargar.TabIndex = 2;
            btnCargar.Text = "Cargar Programa";
            btnCargar.UseVisualStyleBackColor = true;
            btnCargar.Click += btnCargar_Click;
            // 
            // btnGuardarPrograma
            // 
            btnGuardarPrograma.Location = new Point(169, 648);
            btnGuardarPrograma.Name = "btnGuardarPrograma";
            btnGuardarPrograma.Size = new Size(111, 38);
            btnGuardarPrograma.TabIndex = 4;
            btnGuardarPrograma.Text = "Guardar programa";
            btnGuardarPrograma.UseVisualStyleBackColor = true;
            btnGuardarPrograma.Click += btnGuardarPrograma_Click;
            // 
            // btnGuardarArchivo
            // 
            btnGuardarArchivo.Location = new Point(813, 648);
            btnGuardarArchivo.Name = "btnGuardarArchivo";
            btnGuardarArchivo.Size = new Size(111, 38);
            btnGuardarArchivo.TabIndex = 5;
            btnGuardarArchivo.Text = "Guardar archivo";
            btnGuardarArchivo.UseVisualStyleBackColor = true;
            btnGuardarArchivo.Click += btnGuardarArchivo_Click;
            // 
            // dgSimbolos
            // 
            dgSimbolos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgSimbolos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgSimbolos.Columns.AddRange(new DataGridViewColumn[] { ID, Nombre, TipoDato, Valor, Ambito, Direccion, Tamano });
            dgSimbolos.Location = new Point(46, 763);
            dgSimbolos.Name = "dgSimbolos";
            dgSimbolos.RowHeadersVisible = false;
            dgSimbolos.RowHeadersWidth = 62;
            dgSimbolos.Size = new Size(643, 210);
            dgSimbolos.TabIndex = 6;
            // 
            // ID
            // 
            ID.HeaderText = "#";
            ID.MinimumWidth = 8;
            ID.Name = "ID";
            ID.ReadOnly = true;
            // 
            // Nombre
            // 
            Nombre.HeaderText = "Nombre";
            Nombre.MinimumWidth = 8;
            Nombre.Name = "Nombre";
            Nombre.ReadOnly = true;
            // 
            // TipoDato
            // 
            TipoDato.HeaderText = "Tipo";
            TipoDato.MinimumWidth = 8;
            TipoDato.Name = "TipoDato";
            TipoDato.ReadOnly = true;
            // 
            // Valor
            // 
            Valor.HeaderText = "Valor";
            Valor.MinimumWidth = 8;
            Valor.Name = "Valor";
            Valor.ReadOnly = true;
            // 
            // Ambito
            // 
            Ambito.HeaderText = "Ámbito";
            Ambito.MinimumWidth = 8;
            Ambito.Name = "Ambito";
            Ambito.ReadOnly = true;
            // 
            // Direccion
            // 
            Direccion.HeaderText = "Dirección";
            Direccion.MinimumWidth = 8;
            Direccion.Name = "Direccion";
            Direccion.ReadOnly = true;
            // 
            // Tamano
            // 
            Tamano.HeaderText = "Tamaño";
            Tamano.MinimumWidth = 8;
            Tamano.Name = "Tamano";
            Tamano.ReadOnly = true;
            // 
            // dgErrores
            // 
            dgErrores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgErrores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgErrores.Columns.AddRange(new DataGridViewColumn[] { Linea, Error });
            dgErrores.Location = new Point(813, 763);
            dgErrores.Name = "dgErrores";
            dgErrores.RowHeadersVisible = false;
            dgErrores.RowHeadersWidth = 62;
            dgErrores.Size = new Size(643, 210);
            dgErrores.TabIndex = 7;
            // 
            // Linea
            // 
            Linea.FillWeight = 30F;
            Linea.HeaderText = "Línea:Columna";
            Linea.MinimumWidth = 8;
            Linea.Name = "Linea";
            Linea.ReadOnly = true;
            // 
            // Error
            // 
            Error.FillWeight = 149.238586F;
            Error.HeaderText = "Error";
            Error.MinimumWidth = 8;
            Error.Name = "Error";
            Error.ReadOnly = true;
            // 
            // dgErroresSintacticos
            // 
            dgErroresSintacticos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgErroresSintacticos.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgErroresSintacticos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgErroresSintacticos.Columns.AddRange(new DataGridViewColumn[] { LineaSintactica, ErrorSintactico });
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgErroresSintacticos.DefaultCellStyle = dataGridViewCellStyle1;
            dgErroresSintacticos.Location = new Point(47, 1032);
            dgErroresSintacticos.Name = "dgErroresSintacticos";
            dgErroresSintacticos.RowHeadersVisible = false;
            dgErroresSintacticos.RowHeadersWidth = 62;
            dgErroresSintacticos.Size = new Size(1409, 210);
            dgErroresSintacticos.TabIndex = 20;
            // 
            // LineaSintactica
            // 
            LineaSintactica.FillWeight = 30F;
            LineaSintactica.HeaderText = "Línea:Columna";
            LineaSintactica.MinimumWidth = 8;
            LineaSintactica.Name = "LineaSintactica";
            LineaSintactica.ReadOnly = true;
            // 
            // ErrorSintactico
            // 
            ErrorSintactico.FillWeight = 149.238586F;
            ErrorSintactico.HeaderText = "Error";
            ErrorSintactico.MinimumWidth = 8;
            ErrorSintactico.Name = "ErrorSintactico";
            ErrorSintactico.ReadOnly = true;
            // 
            // lblLenguaje
            // 
            lblLenguaje.AutoSize = true;
            lblLenguaje.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblLenguaje.ForeColor = SystemColors.Highlight;
            lblLenguaje.Location = new Point(194, 53);
            lblLenguaje.Name = "lblLenguaje";
            lblLenguaje.Size = new Size(226, 48);
            lblLenguaje.TabIndex = 8;
            lblLenguaje.Text = "Banana v0.1";
            lblLenguaje.Click += lblLenguaje_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = (Image)resources.GetObject("pictureBox1.InitialImage");
            pictureBox1.Location = new Point(46, 18);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(143, 168);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = SystemColors.Control;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Location = new Point(1110, 53);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(344, 50);
            richTextBox1.TabIndex = 12;
            richTextBox1.Text = "\n";
            // 
            // lblCodigo
            // 
            lblCodigo.AutoSize = true;
            lblCodigo.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCodigo.Location = new Point(46, 222);
            lblCodigo.Margin = new Padding(4, 0, 4, 0);
            lblCodigo.Name = "lblCodigo";
            lblCodigo.Size = new Size(236, 40);
            lblCodigo.TabIndex = 13;
            lblCodigo.Text = "Programa Fuente";
            // 
            // lblTokens
            // 
            lblTokens.AutoSize = true;
            lblTokens.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTokens.Location = new Point(813, 222);
            lblTokens.Margin = new Padding(4, 0, 4, 0);
            lblTokens.Name = "lblTokens";
            lblTokens.Size = new Size(247, 40);
            lblTokens.TabIndex = 14;
            lblTokens.Text = "Archivo de Tokens";
            // 
            // lblTabla
            // 
            lblTabla.AutoSize = true;
            lblTabla.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTabla.Location = new Point(46, 718);
            lblTabla.Margin = new Padding(4, 0, 4, 0);
            lblTabla.Name = "lblTabla";
            lblTabla.Size = new Size(248, 40);
            lblTabla.TabIndex = 15;
            lblTabla.Text = "Tabla de Símbolos";
            // 
            // lblErrores
            // 
            lblErrores.AutoSize = true;
            lblErrores.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrores.Location = new Point(813, 718);
            lblErrores.Margin = new Padding(4, 0, 4, 0);
            lblErrores.Name = "lblErrores";
            lblErrores.Size = new Size(205, 40);
            lblErrores.TabIndex = 16;
            lblErrores.Text = "Errores Léxicos";
            // 
            // lblErroresSintacticos
            // 
            lblErroresSintacticos.AutoSize = true;
            lblErroresSintacticos.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErroresSintacticos.Location = new Point(47, 987);
            lblErroresSintacticos.Margin = new Padding(4, 0, 4, 0);
            lblErroresSintacticos.Name = "lblErroresSintacticos";
            lblErroresSintacticos.Size = new Size(249, 40);
            lblErroresSintacticos.TabIndex = 21;
            lblErroresSintacticos.Text = "Errores Sintácticos";
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(201, 107);
            lblSubtitle.Margin = new Padding(4, 0, 4, 0);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(336, 25);
            lblSubtitle.TabIndex = 17;
            lblSubtitle.Text = "Analizador Léxico, Sintáctico y Semántico";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(46, 1265);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(117, 40);
            label1.TabIndex = 18;
            label1.Text = "Reporte";
            // 
            // txtReporte
            // 
            txtReporte.Location = new Point(47, 1312);
            txtReporte.Margin = new Padding(4, 5, 4, 5);
            txtReporte.Name = "txtReporte";
            txtReporte.ReadOnly = true;
            txtReporte.Size = new Size(1407, 97);
            txtReporte.TabIndex = 19;
            txtReporte.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(1120, 72);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(334, 50);
            label2.TabIndex = 22;
            label2.Text = "23100147 Marco Antonio Celis Mata\r\n23100218 Cesar Alejandro Vazquez Soto";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1798, 1427);
            Controls.Add(label2);
            Controls.Add(txtReporte);
            Controls.Add(label1);
            Controls.Add(lblSubtitle);
            Controls.Add(lblErroresSintacticos);
            Controls.Add(lblErrores);
            Controls.Add(lblTabla);
            Controls.Add(lblTokens);
            Controls.Add(lblCodigo);
            Controls.Add(richTextBox1);
            Controls.Add(pictureBox1);
            Controls.Add(lblLenguaje);
            Controls.Add(dgErroresSintacticos);
            Controls.Add(dgErrores);
            Controls.Add(dgSimbolos);
            Controls.Add(btnGuardarArchivo);
            Controls.Add(btnGuardarPrograma);
            Controls.Add(btnCargar);
            Controls.Add(txtTokens);
            Controls.Add(txtCode);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Banana v0.0.1 - Analizador Léxico y Sintáctico";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dgSimbolos).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgErrores).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgErroresSintacticos).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ScintillaNET.Scintilla txtCode;
        private ScintillaNET.Scintilla txtTokens;
        private Button btnCargar;
        private Button btnGuardarPrograma;
        private Button btnGuardarArchivo;
        private DataGridView dgSimbolos;
        private DataGridView dgErrores;
        private DataGridView dgErroresSintacticos;
        private Label lblLenguaje;
        private PictureBox pictureBox1;
        private RichTextBox richTextBox1;
        private Label lblCodigo;
        private Label lblTokens;
        private Label lblTabla;
        private Label lblErrores;
        private Label lblErroresSintacticos;
        private Label lblSubtitle;
        private DataGridViewTextBoxColumn Linea;
        private DataGridViewTextBoxColumn Error;
        private DataGridViewTextBoxColumn LineaSintactica;
        private DataGridViewTextBoxColumn ErrorSintactico;
        private Label label1;
        private RichTextBox txtReporte;
        private Label label2;
        private DataGridViewTextBoxColumn ID;
        private DataGridViewTextBoxColumn Nombre;
        private DataGridViewTextBoxColumn TipoDato;
        private DataGridViewTextBoxColumn Valor;
        private DataGridViewTextBoxColumn Ambito;
        private DataGridViewTextBoxColumn Direccion;
        private DataGridViewTextBoxColumn Tamano;
    }
}
