using BananaCompiler.Lexer;
using BananaCompiler.Models;
using BananaCompiler.Parser;
using ScintillaNET;
using System.Diagnostics;

namespace BananaCompiler
{
    public partial class Form1 : Form
    {

        private TransitionTable transitionTable = null!;
        internal Dictionary<string, Identifier> Identifiers = [];
        private List<Token> lexicalTokens = [];

        public Form1()
        {
            InitializeComponent();

            txtCode.Margins[0].Width = 16;
            txtTokens.Margins[0].Width = 16;
            txtCode.TabWidth = 2;
            txtTokens.TabWidth = 2;
            lblErroresSintacticos.Text = "Errores Sintácticos y Semánticos";
            dgSimbolos.AllowUserToAddRows = false;
            dgErrores.AllowUserToAddRows = false;
            dgErroresSintacticos.AllowUserToAddRows = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            transitionTable = SqliteTransitionTableSource.Load("data.db");
        }

        private void handleToken(string token)
        {
            txtTokens.AddText(token + ' ');
        }

        private void handleIdToken(string token, string lexeme)
        {
            if (!Identifiers.TryGetValue(lexeme, out Identifier? value))
            {
                var identificador = new Identifier()
                {
                    Id = Identifiers.Count + 1,
                    Name = lexeme
                };
                value = identificador;
                Identifiers[lexeme] = value;
                dgSimbolos.Rows.Add(identificador.Id, lexeme);

            }
            txtTokens.AddText(token + value.Id + ' ');
        }

        private void applySymbolAssignments(IReadOnlyDictionary<string, SymbolAssignment> assignments)
        {
            foreach (SymbolAssignment assignment in assignments.Values)
            {
                if (!Identifiers.TryGetValue(assignment.Name, out Identifier? identifier))
                    continue;

                identifier.DataType = assignment.DataType;
                identifier.Value = assignment.Value;
                identifier.Scope = assignment.Scope;
                identifier.Address = assignment.Address;
                identifier.Size = assignment.Size;
            }

            if (assignments.Count > 0)
                updateSimbolos();
        }

        private void updateSimbolos()
        {
            dgSimbolos.Rows.Clear();
            foreach (var (_, id) in Identifiers.OrderBy(pair => pair.Value.Address))
            {
                dgSimbolos.Rows.Add(id.Id, id.Name, id.DataType, id.Value, id.Scope, id.AddressHex, id.Size);
            }
        }

        private void handleErrorToken(int line, string data)
        {
            dgErrores.Rows.Add(line, data);
            btnGuardarArchivo.Enabled = false;
        }

        private void onToken(Token token)
        {
            switch (token.Kind)
            {
                case TokenKinds.Identifier:
                    lexicalTokens.Add(token);
                    handleIdToken(token.Kind, token.Lexeme);
                    break;
                case TokenKinds.Error:
                    handleErrorToken(token.Line, token.Lexeme);
                    handleToken(token.Kind);
                    break;
                default:
                    lexicalTokens.Add(token);
                    handleToken(token.Kind);
                    break;
            }
        }

        private int maxLineNumberCharLength;

        private void txtCode_TextChanged_Lines(object sender, EventArgs e)
        {
            var maxLineNumberCharLength = txtCode.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == this.maxLineNumberCharLength)
                return;
            const int padding = 2;
            txtCode.Margins[0].Width = txtCode.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            this.maxLineNumberCharLength = maxLineNumberCharLength;
        }

        private void txtTokens_TextChanged_Lines(object sender, EventArgs e)
        {
            var maxLineNumberCharLength = txtTokens.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == this.maxLineNumberCharLength)
                return;
            const int padding = 2;
            txtTokens.Margins[0].Width = txtTokens.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            this.maxLineNumberCharLength = maxLineNumberCharLength;
        }

        private void highlightErrors(Scintilla scintilla)
        {
            string text = scintilla.Text;
            // Disable lexer (new API)
            scintilla.LexerName = null;
            // Define style (red)
            scintilla.Styles[1].ForeColor = Color.Red;
            int index = 0;
            while ((index = text.IndexOf(TokenKinds.Error, index)) != -1)
            {
                scintilla.StartStyling(index);
                scintilla.SetStyling(TokenKinds.Error.Length, 1);

                index += TokenKinds.Error.Length;
            }
        }

        private void clearAll()
        {
            Identifiers = [];
            lexicalTokens = [];
            txtTokens.Text = "";
            btnGuardarArchivo.Enabled = true;
            dgSimbolos.Rows.Clear();
            dgErrores.Rows.Clear();
            dgErroresSintacticos.Rows.Clear();
        }

        private void txtCode_TextChanged(object sender, EventArgs e)
        {
            var sw = Stopwatch.StartNew();
            clearAll();

            var lexer = new LexicalAnalyzer(transitionTable);
            var tokens = lexer.Tokenize(txtCode.Text);
            var tokenIndex = 0;

            for (int i = 0; i < txtCode.Lines.Count; i++)
            {
                var lineNumber = i + 1;
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Line == lineNumber)
                {
                    onToken(tokens[tokenIndex]);
                    tokenIndex++;
                }

                if (i != txtCode.Lines.Count - 1)
                    txtTokens.AddText(Environment.NewLine);
            }

            highlightErrors(txtTokens);

            var lexicalErrorCount = countRows(dgErrores);
            var syntaxErrorCount = 0;
            string treeInfo = "Árbol: (sin analizador sintáctico)";
            string pileInfo = "Pila semántica: (sin datos)";
            if (lexicalErrorCount == 0)
            {
                syntaxErrorCount = analyzeSyntax(out treeInfo, out pileInfo);
            }

            txtReporte.Text = @$"Líneas de código: {txtTokens.Lines.Count}
Cantidad Símbolos: {Identifiers.Count}
Errores Léxicos: {lexicalErrorCount}
Errores Sintácticos y Semánticos: {syntaxErrorCount}
{treeInfo}
{pileInfo}";
            var elapsed = sw.Elapsed.TotalMilliseconds;
            Debug.WriteLine(elapsed.ToString());
        }

        private int analyzeSyntax(out string treeInfo, out string pileInfo)
        {
            var parser = new TopDownParser(lexicalTokens);
            var result = parser.Parse();
            applySymbolAssignments(result.SymbolAssignments);

            foreach (var error in result.Errors)
                dgErroresSintacticos.Rows.Add(error.Token.Line, error.Format());

            foreach (SemanticError error in result.SemanticErrors)
                dgErroresSintacticos.Rows.Add(error.Token.Line, $"Semántico: {error.Format()}");

            treeInfo = result.SyntaxTree is null
                ? "Árbol: no construido"
                : $"Árbol: raíz {result.SyntaxTree.Kind} ({CountNodes(result.SyntaxTree)} nodos)";
            pileInfo = $"Pila semántica: {result.SemanticPileLog.Count} reducciones";

            return result.Errors.Count + result.SemanticErrors.Count;
        }

        private static int CountNodes(SyntaxNode node)
        {
            int count = 1;
            foreach (SyntaxNode child in node.Children)
                count += CountNodes(child);
            return count;
        }

        private static int countRows(DataGridView grid)
        {
            return grid.Rows.Cast<DataGridViewRow>().Count(row => !row.IsNewRow);
        }

        private string? loadTextFromFile()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Banana files (*.banana)|*.banana|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.Title = "Open a Banana File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return File.ReadAllText(openFileDialog.FileName);
            }

            return null;
        }

        private void saveTextToFile(string title, string content)
        {
            using var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.Title = title;
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, content);
            }
        }

        private void btnCargar_Click(object sender, EventArgs e)
        {
            var content = loadTextFromFile();
            if (content != null)
            {
                txtCode.Text = content;
            }
        }

        private void btnGuardarPrograma_Click(object sender, EventArgs e)
        {
            saveTextToFile("Guardar programa", txtCode.Text);
        }

        private void btnGuardarArchivo_Click(object sender, EventArgs e)
        {
            saveTextToFile("Guardar archivo de tokens", txtTokens.Text);
        }
    }
}
