namespace BananaCompiler
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
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
            txtCode.Location = new Point(32, 160);
            txtCode.Margin = new Padding(2, 2, 2, 2);
            txtCode.Name = "txtCode";
            txtCode.ScrollWidth = 74;
            txtCode.Size = new Size(450, 225);
            txtCode.TabIndex = 0;
            txtCode.Text = "start\r\n\r\nend";
            txtCode.TextChanged += txtCode_TextChanged;
            // 
            // txtTokens
            // 
            txtTokens.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            txtTokens.Font = new Font("Consolas", 9.75F);
            txtTokens.LexerName = null;
            txtTokens.Location = new Point(569, 160);
            txtTokens.Margin = new Padding(2, 2, 2, 2);
            txtTokens.Name = "txtTokens";
            txtTokens.ScrollWidth = 74;
            txtTokens.Size = new Size(450, 225);
            txtTokens.TabIndex = 1;
            txtTokens.TextChanged += txtTokens_TextChanged_Lines;
            // 
            // btnCargar
            // 
            btnCargar.Location = new Point(32, 389);
            btnCargar.Margin = new Padding(2, 2, 2, 2);
            btnCargar.Name = "btnCargar";
            btnCargar.Size = new Size(78, 23);
            btnCargar.TabIndex = 2;
            btnCargar.Text = "Cargar Programa";
            btnCargar.UseVisualStyleBackColor = true;
            btnCargar.Click += btnCargar_Click;
            // 
            // btnGuardarPrograma
            // 
            btnGuardarPrograma.Location = new Point(118, 389);
            btnGuardarPrograma.Margin = new Padding(2, 2, 2, 2);
            btnGuardarPrograma.Name = "btnGuardarPrograma";
            btnGuardarPrograma.Size = new Size(78, 23);
            btnGuardarPrograma.TabIndex = 4;
            btnGuardarPrograma.Text = "Guardar programa";
            btnGuardarPrograma.UseVisualStyleBackColor = true;
            btnGuardarPrograma.Click += btnGuardarPrograma_Click;
            // 
            // btnGuardarArchivo
            // 
            btnGuardarArchivo.Location = new Point(569, 389);
            btnGuardarArchivo.Margin = new Padding(2, 2, 2, 2);
            btnGuardarArchivo.Name = "btnGuardarArchivo";
            btnGuardarArchivo.Size = new Size(78, 23);
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
            dgSimbolos.Location = new Point(32, 458);
            dgSimbolos.Margin = new Padding(2, 2, 2, 2);
            dgSimbolos.Name = "dgSimbolos";
            dgSimbolos.RowHeadersVisible = false;
            dgSimbolos.RowHeadersWidth = 62;
            dgSimbolos.Size = new Size(450, 126);
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
            TipoDato.HeaderText = "Tipo de dato";
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
            Tamano.HeaderText = "Tamaño (bytes)";
            Tamano.MinimumWidth = 8;
            Tamano.Name = "Tamano";
            Tamano.ReadOnly = true;
            // 
            // dgErrores
            // 
            dgErrores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgErrores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgErrores.Columns.AddRange(new DataGridViewColumn[] { Linea, Error });
            dgErrores.Location = new Point(569, 458);
            dgErrores.Margin = new Padding(2, 2, 2, 2);
            dgErrores.Name = "dgErrores";
            dgErrores.RowHeadersVisible = false;
            dgErrores.RowHeadersWidth = 62;
            dgErrores.Size = new Size(450, 126);
            dgErrores.TabIndex = 7;
            // 
            // Linea
            // 
            Linea.FillWeight = 30F;
            Linea.HeaderText = "Linea";
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
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dgErroresSintacticos.DefaultCellStyle = dataGridViewCellStyle2;
            dgErroresSintacticos.Location = new Point(33, 619);
            dgErroresSintacticos.Margin = new Padding(2, 2, 2, 2);
            dgErroresSintacticos.Name = "dgErroresSintacticos";
            dgErroresSintacticos.RowHeadersVisible = false;
            dgErroresSintacticos.RowHeadersWidth = 62;
            dgErroresSintacticos.Size = new Size(986, 126);
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
            lblLenguaje.Location = new Point(136, 32);
            lblLenguaje.Margin = new Padding(2, 0, 2, 0);
            lblLenguaje.Name = "lblLenguaje";
            lblLenguaje.Size = new Size(174, 32);
            lblLenguaje.TabIndex = 8;
            lblLenguaje.Text = "Banana v0.0.1";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = (Image)resources.GetObject("pictureBox1.InitialImage");
            pictureBox1.Location = new Point(32, 11);
            pictureBox1.Margin = new Padding(2, 2, 2, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(100, 101);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = SystemColors.Control;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Location = new Point(735, 32);
            richTextBox1.Margin = new Padding(2, 2, 2, 2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(283, 67);
            richTextBox1.TabIndex = 12;
            richTextBox1.Text = "\n";
            // 
            // lblCodigo
            // 
            lblCodigo.AutoSize = true;
            lblCodigo.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCodigo.Location = new Point(32, 133);
            lblCodigo.Name = "lblCodigo";
            lblCodigo.Size = new Size(157, 25);
            lblCodigo.TabIndex = 13;
            lblCodigo.Text = "Programa Fuente";
            // 
            // lblTokens
            // 
            lblTokens.AutoSize = true;
            lblTokens.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTokens.Location = new Point(569, 133);
            lblTokens.Name = "lblTokens";
            lblTokens.Size = new Size(164, 25);
            lblTokens.TabIndex = 14;
            lblTokens.Text = "Archivo de Tokens";
            // 
            // lblTabla
            // 
            lblTabla.AutoSize = true;
            lblTabla.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTabla.Location = new Point(32, 431);
            lblTabla.Name = "lblTabla";
            lblTabla.Size = new Size(164, 25);
            lblTabla.TabIndex = 15;
            lblTabla.Text = "Tabla de Símbolos";
            // 
            // lblErrores
            // 
            lblErrores.AutoSize = true;
            lblErrores.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrores.Location = new Point(569, 431);
            lblErrores.Name = "lblErrores";
            lblErrores.Size = new Size(138, 25);
            lblErrores.TabIndex = 16;
            lblErrores.Text = "Errores Léxicos";
            // 
            // lblErroresSintacticos
            // 
            lblErroresSintacticos.AutoSize = true;
            lblErroresSintacticos.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErroresSintacticos.Location = new Point(33, 592);
            lblErroresSintacticos.Name = "lblErroresSintacticos";
            lblErroresSintacticos.Size = new Size(167, 25);
            lblErroresSintacticos.TabIndex = 21;
            lblErroresSintacticos.Text = "Errores Sintácticos";
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(141, 64);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(163, 15);
            lblSubtitle.TabIndex = 17;
            lblSubtitle.Text = "Analizador Léxico y Sintáctico";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(32, 759);
            label1.Name = "label1";
            label1.Size = new Size(77, 25);
            label1.TabIndex = 18;
            label1.Text = "Reporte";
            // 
            // txtReporte
            // 
            txtReporte.Location = new Point(33, 787);
            txtReporte.Name = "txtReporte";
            txtReporte.ReadOnly = true;
            txtReporte.Size = new Size(986, 60);
            txtReporte.TabIndex = 19;
            txtReporte.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1050, 637);
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
            Margin = new Padding(2, 2, 2, 2);
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
        private DataGridViewTextBoxColumn ID;
        private DataGridViewTextBoxColumn Nombre;
        private DataGridViewTextBoxColumn TipoDato;
        private DataGridViewTextBoxColumn Valor;
        private DataGridViewTextBoxColumn Ambito;
        private DataGridViewTextBoxColumn Direccion;
        private DataGridViewTextBoxColumn Tamano;
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
    }
}
