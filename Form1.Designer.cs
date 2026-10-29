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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            splitArbolNotaciones = new SplitContainer();
            tvArbolSintactico = new TreeView();
            tabNotaciones = new TabControl();
            tabPagePrefija = new TabPage();
            rtbNotacionPrefija = new RichTextBox();
            tabPageInfija = new TabPage();
            txtInfija = new RichTextBox();
            tabPagePostfija = new TabPage();
            rtbNotacionPostfija = new RichTextBox();
            tabPage2 = new TabPage();
            splitCuadruplos = new SplitContainer();
            dgCuadruplos = new DataGridView();
            IndiceCuadruplo = new DataGridViewTextBoxColumn();
            OpCuadruplo = new DataGridViewTextBoxColumn();
            Arg1Cuadruplo = new DataGridViewTextBoxColumn();
            Arg2Cuadruplo = new DataGridViewTextBoxColumn();
            ResultadoCuadruplo = new DataGridViewTextBoxColumn();
            rtbCodigo3D = new RichTextBox();
            tabPage3 = new TabPage();
            txtCodigoP = new RichTextBox();
            tabPage4 = new TabPage();
            rtbPilaSemantica = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)dgSimbolos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgErrores).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgErroresSintacticos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitArbolNotaciones).BeginInit();
            splitArbolNotaciones.Panel1.SuspendLayout();
            splitArbolNotaciones.Panel2.SuspendLayout();
            splitArbolNotaciones.SuspendLayout();
            tabNotaciones.SuspendLayout();
            tabPagePrefija.SuspendLayout();
            tabPageInfija.SuspendLayout();
            tabPagePostfija.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitCuadruplos).BeginInit();
            splitCuadruplos.Panel1.SuspendLayout();
            splitCuadruplos.Panel2.SuspendLayout();
            splitCuadruplos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgCuadruplos).BeginInit();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            SuspendLayout();
            // 
            // txtCode
            // 
            txtCode.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            txtCode.Font = new Font("Consolas", 9.75F);
            txtCode.LexerName = null;
            txtCode.Location = new Point(32, 160);
            txtCode.Margin = new Padding(2);
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
            txtTokens.Margin = new Padding(2);
            txtTokens.Name = "txtTokens";
            txtTokens.ScrollWidth = 74;
            txtTokens.Size = new Size(450, 225);
            txtTokens.TabIndex = 1;
            txtTokens.TextChanged += txtTokens_TextChanged_Lines;
            // 
            // btnCargar
            // 
            btnCargar.Location = new Point(32, 389);
            btnCargar.Margin = new Padding(2);
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
            btnGuardarPrograma.Margin = new Padding(2);
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
            btnGuardarArchivo.Margin = new Padding(2);
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
            dgSimbolos.Margin = new Padding(2);
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
            dgErrores.Location = new Point(569, 458);
            dgErrores.Margin = new Padding(2);
            dgErrores.Name = "dgErrores";
            dgErrores.RowHeadersVisible = false;
            dgErrores.RowHeadersWidth = 62;
            dgErrores.Size = new Size(450, 126);
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
            dgErroresSintacticos.Location = new Point(33, 619);
            dgErroresSintacticos.Margin = new Padding(2);
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
            lblLenguaje.Size = new Size(153, 32);
            lblLenguaje.TabIndex = 8;
            lblLenguaje.Text = "Banana v0.1";
            lblLenguaje.Click += lblLenguaje_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = (Image)resources.GetObject("pictureBox1.InitialImage");
            pictureBox1.Location = new Point(32, 11);
            pictureBox1.Margin = new Padding(2);
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
            richTextBox1.Location = new Point(777, 32);
            richTextBox1.Margin = new Padding(2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(241, 30);
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
            lblSubtitle.Size = new Size(225, 15);
            lblSubtitle.TabIndex = 17;
            lblSubtitle.Text = "Analizador Léxico, Sintáctico y Semántico";
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
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(784, 43);
            label2.Name = "label2";
            label2.Size = new Size(213, 30);
            label2.TabIndex = 22;
            label2.Text = "23100147 Marco Antonio Celis Mata\r\n23100218 Cesar Alejandro Vazquez Soto";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(1045, 160);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(634, 789);
            tabControl1.TabIndex = 23;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(splitArbolNotaciones);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(626, 761);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Arbol y Notaciones";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitArbolNotaciones
            // 
            splitArbolNotaciones.Dock = DockStyle.Fill;
            splitArbolNotaciones.Location = new Point(3, 3);
            splitArbolNotaciones.Name = "splitArbolNotaciones";
            splitArbolNotaciones.Orientation = Orientation.Horizontal;
            // 
            // splitArbolNotaciones.Panel1
            // 
            splitArbolNotaciones.Panel1.Controls.Add(tvArbolSintactico);
            // 
            // splitArbolNotaciones.Panel2
            // 
            splitArbolNotaciones.Panel2.Controls.Add(tabNotaciones);
            splitArbolNotaciones.Size = new Size(620, 755);
            splitArbolNotaciones.SplitterDistance = 438;
            splitArbolNotaciones.TabIndex = 0;
            // 
            // tvArbolSintactico
            // 
            tvArbolSintactico.Dock = DockStyle.Fill;
            tvArbolSintactico.HideSelection = false;
            tvArbolSintactico.Location = new Point(0, 0);
            tvArbolSintactico.Name = "tvArbolSintactico";
            tvArbolSintactico.ShowNodeToolTips = true;
            tvArbolSintactico.Size = new Size(620, 438);
            tvArbolSintactico.TabIndex = 0;
            // 
            // tabNotaciones
            // 
            tabNotaciones.Controls.Add(tabPagePrefija);
            tabNotaciones.Controls.Add(tabPageInfija);
            tabNotaciones.Controls.Add(tabPagePostfija);
            tabNotaciones.Dock = DockStyle.Fill;
            tabNotaciones.Location = new Point(0, 0);
            tabNotaciones.Name = "tabNotaciones";
            tabNotaciones.SelectedIndex = 0;
            tabNotaciones.Size = new Size(620, 313);
            tabNotaciones.TabIndex = 0;
            // 
            // tabPagePrefija
            // 
            tabPagePrefija.Controls.Add(rtbNotacionPrefija);
            tabPagePrefija.Location = new Point(4, 24);
            tabPagePrefija.Name = "tabPagePrefija";
            tabPagePrefija.Padding = new Padding(3);
            tabPagePrefija.Size = new Size(612, 285);
            tabPagePrefija.TabIndex = 0;
            tabPagePrefija.Text = "Prefija (preorden)";
            tabPagePrefija.UseVisualStyleBackColor = true;
            // 
            // rtbNotacionPrefija
            // 
            rtbNotacionPrefija.BackColor = SystemColors.Window;
            rtbNotacionPrefija.Dock = DockStyle.Fill;
            rtbNotacionPrefija.Font = new Font("Consolas", 9.75F);
            rtbNotacionPrefija.Location = new Point(3, 3);
            rtbNotacionPrefija.Name = "rtbNotacionPrefija";
            rtbNotacionPrefija.ReadOnly = true;
            rtbNotacionPrefija.Size = new Size(606, 279);
            rtbNotacionPrefija.TabIndex = 0;
            rtbNotacionPrefija.Text = "";
            // 
            // tabPageInfija
            // 
            tabPageInfija.Controls.Add(txtInfija);
            tabPageInfija.Location = new Point(4, 24);
            tabPageInfija.Name = "tabPageInfija";
            tabPageInfija.Padding = new Padding(3);
            tabPageInfija.Size = new Size(403, 185);
            tabPageInfija.TabIndex = 1;
            tabPageInfija.Text = "Infija (inorden)";
            tabPageInfija.UseVisualStyleBackColor = true;
            // 
            // txtInfija
            // 
            txtInfija.BackColor = SystemColors.Window;
            txtInfija.Dock = DockStyle.Fill;
            txtInfija.Font = new Font("Consolas", 9.75F);
            txtInfija.Location = new Point(3, 3);
            txtInfija.Name = "txtInfija";
            txtInfija.ReadOnly = true;
            txtInfija.Size = new Size(397, 179);
            txtInfija.TabIndex = 0;
            txtInfija.Text = "";
            // 
            // tabPagePostfija
            // 
            tabPagePostfija.Controls.Add(rtbNotacionPostfija);
            tabPagePostfija.Location = new Point(4, 24);
            tabPagePostfija.Name = "tabPagePostfija";
            tabPagePostfija.Padding = new Padding(3);
            tabPagePostfija.Size = new Size(403, 185);
            tabPagePostfija.TabIndex = 2;
            tabPagePostfija.Text = "Postfija (posfijo)";
            tabPagePostfija.UseVisualStyleBackColor = true;
            // 
            // rtbNotacionPostfija
            // 
            rtbNotacionPostfija.BackColor = SystemColors.Window;
            rtbNotacionPostfija.Dock = DockStyle.Fill;
            rtbNotacionPostfija.Font = new Font("Consolas", 9.75F);
            rtbNotacionPostfija.Location = new Point(3, 3);
            rtbNotacionPostfija.Name = "rtbNotacionPostfija";
            rtbNotacionPostfija.ReadOnly = true;
            rtbNotacionPostfija.Size = new Size(397, 179);
            rtbNotacionPostfija.TabIndex = 0;
            rtbNotacionPostfija.Text = "";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(splitCuadruplos);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(417, 523);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Cuádruplos y Código 3D";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitCuadruplos
            // 
            splitCuadruplos.Dock = DockStyle.Fill;
            splitCuadruplos.Location = new Point(3, 3);
            splitCuadruplos.Name = "splitCuadruplos";
            splitCuadruplos.Orientation = Orientation.Horizontal;
            // 
            // splitCuadruplos.Panel1
            // 
            splitCuadruplos.Panel1.Controls.Add(dgCuadruplos);
            // 
            // splitCuadruplos.Panel2
            // 
            splitCuadruplos.Panel2.Controls.Add(rtbCodigo3D);
            splitCuadruplos.Size = new Size(411, 517);
            splitCuadruplos.SplitterDistance = 250;
            splitCuadruplos.TabIndex = 0;
            // 
            // dgCuadruplos
            // 
            dgCuadruplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgCuadruplos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgCuadruplos.Columns.AddRange(new DataGridViewColumn[] { IndiceCuadruplo, OpCuadruplo, Arg1Cuadruplo, Arg2Cuadruplo, ResultadoCuadruplo });
            dgCuadruplos.Dock = DockStyle.Fill;
            dgCuadruplos.Location = new Point(0, 0);
            dgCuadruplos.Margin = new Padding(2);
            dgCuadruplos.Name = "dgCuadruplos";
            dgCuadruplos.RowHeadersVisible = false;
            dgCuadruplos.RowHeadersWidth = 62;
            dgCuadruplos.Size = new Size(411, 250);
            dgCuadruplos.TabIndex = 0;
            // 
            // IndiceCuadruplo
            // 
            IndiceCuadruplo.FillWeight = 25F;
            IndiceCuadruplo.HeaderText = "#";
            IndiceCuadruplo.MinimumWidth = 8;
            IndiceCuadruplo.Name = "IndiceCuadruplo";
            IndiceCuadruplo.ReadOnly = true;
            // 
            // OpCuadruplo
            // 
            OpCuadruplo.HeaderText = "Op";
            OpCuadruplo.MinimumWidth = 8;
            OpCuadruplo.Name = "OpCuadruplo";
            OpCuadruplo.ReadOnly = true;
            // 
            // Arg1Cuadruplo
            // 
            Arg1Cuadruplo.HeaderText = "Arg1";
            Arg1Cuadruplo.MinimumWidth = 8;
            Arg1Cuadruplo.Name = "Arg1Cuadruplo";
            Arg1Cuadruplo.ReadOnly = true;
            // 
            // Arg2Cuadruplo
            // 
            Arg2Cuadruplo.HeaderText = "Arg2";
            Arg2Cuadruplo.MinimumWidth = 8;
            Arg2Cuadruplo.Name = "Arg2Cuadruplo";
            Arg2Cuadruplo.ReadOnly = true;
            // 
            // ResultadoCuadruplo
            // 
            ResultadoCuadruplo.HeaderText = "Resultado";
            ResultadoCuadruplo.MinimumWidth = 8;
            ResultadoCuadruplo.Name = "ResultadoCuadruplo";
            ResultadoCuadruplo.ReadOnly = true;
            // 
            // rtbCodigo3D
            // 
            rtbCodigo3D.BackColor = SystemColors.Window;
            rtbCodigo3D.Dock = DockStyle.Fill;
            rtbCodigo3D.Font = new Font("Consolas", 9.75F);
            rtbCodigo3D.Location = new Point(0, 0);
            rtbCodigo3D.Name = "rtbCodigo3D";
            rtbCodigo3D.ReadOnly = true;
            rtbCodigo3D.Size = new Size(411, 263);
            rtbCodigo3D.TabIndex = 0;
            rtbCodigo3D.Text = "";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(txtCodigoP);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(417, 523);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Código P";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // txtCodigoP
            // 
            txtCodigoP.BackColor = SystemColors.Window;
            txtCodigoP.Dock = DockStyle.Fill;
            txtCodigoP.Font = new Font("Consolas", 9.75F);
            txtCodigoP.Location = new Point(3, 3);
            txtCodigoP.Name = "txtCodigoP";
            txtCodigoP.ReadOnly = true;
            txtCodigoP.Size = new Size(411, 517);
            txtCodigoP.TabIndex = 0;
            txtCodigoP.Text = "";
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(rtbPilaSemantica);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(417, 523);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Pila Semántica";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // rtbPilaSemantica
            // 
            rtbPilaSemantica.BackColor = SystemColors.Window;
            rtbPilaSemantica.Dock = DockStyle.Fill;
            rtbPilaSemantica.Font = new Font("Consolas", 9.75F);
            rtbPilaSemantica.Location = new Point(3, 3);
            rtbPilaSemantica.Name = "rtbPilaSemantica";
            rtbPilaSemantica.ReadOnly = true;
            rtbPilaSemantica.Size = new Size(411, 517);
            rtbPilaSemantica.TabIndex = 0;
            rtbPilaSemantica.Text = "";
            rtbPilaSemantica.WordWrap = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1703, 974);
            Controls.Add(tabControl1);
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
            Margin = new Padding(2);
            Name = "Form1";
            Text = "Banana v0.0.1 - Analizador Léxico y Sintáctico";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dgSimbolos).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgErrores).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgErroresSintacticos).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            splitArbolNotaciones.Panel1.ResumeLayout(false);
            splitArbolNotaciones.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitArbolNotaciones).EndInit();
            splitArbolNotaciones.ResumeLayout(false);
            tabNotaciones.ResumeLayout(false);
            tabPagePrefija.ResumeLayout(false);
            tabPageInfija.ResumeLayout(false);
            tabPagePostfija.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            splitCuadruplos.Panel1.ResumeLayout(false);
            splitCuadruplos.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitCuadruplos).EndInit();
            splitCuadruplos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgCuadruplos).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage4.ResumeLayout(false);
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
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private SplitContainer splitArbolNotaciones;
        private TabControl tabNotaciones;
        private TabPage tabPagePrefija;
        private TabPage tabPageInfija;
        private TabPage tabPagePostfija;
        private RichTextBox rtbNotacionPrefija;
        private RichTextBox txtInfija;
        private RichTextBox rtbNotacionPostfija;
        private TreeView tvArbolSintactico;
        private SplitContainer splitCuadruplos;
        private DataGridView dgCuadruplos;
        private DataGridViewTextBoxColumn IndiceCuadruplo;
        private DataGridViewTextBoxColumn OpCuadruplo;
        private DataGridViewTextBoxColumn Arg1Cuadruplo;
        private DataGridViewTextBoxColumn Arg2Cuadruplo;
        private DataGridViewTextBoxColumn ResultadoCuadruplo;
        private RichTextBox rtbCodigo3D;
        private RichTextBox txtCodigoP;
        private RichTextBox rtbPilaSemantica;
    }
}
