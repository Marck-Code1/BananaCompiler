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

        private void updateSimbolos(IReadOnlyList<SymbolAssignment> symbols)
        {
            dgSimbolos.Rows.Clear();
            var displayedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (SymbolAssignment symbol in symbols.OrderBy(symbol => symbol.Address))
            {
                if (!Identifiers.TryGetValue(symbol.Name, out Identifier? identifier))
                    continue;

                dgSimbolos.Rows.Add(identifier.Id, symbol.Name, symbol.DataType, symbol.Value,
                    symbol.Scope, symbol.AddressHex, symbol.Size);
                displayedNames.Add(symbol.Name);
            }

            // Conserva los identificadores léxicos no declarados o pendientes por errores.
            foreach (Identifier identifier in Identifiers.Values.OrderBy(identifier => identifier.Id))
            {
                if (!displayedNames.Contains(identifier.Name))
                    dgSimbolos.Rows.Add(identifier.Id, identifier.Name);
            }
        }

        private void handleErrorToken(int line, int column, string data)
        {
            dgErrores.Rows.Add($"{line}:{column}", data);
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
                    handleErrorToken(token.Line, token.Column, token.Lexeme);
                    handleToken(token.Kind);
                    break;
                case TokenKinds.EndOfFile:
                    lexicalTokens.Add(token);
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
            scintilla.LexerName = null;
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
            var tokens = lexer.Tokenize(txtCode.Text, includeEndOfFile: true);
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
Cantidad Símbolos: {countRows(dgSimbolos)}
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
            updateSimbolos(result.ScopedSymbols);

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                dgErroresSintacticos.Rows.Add($"{diagnostic.Token.Line}:{diagnostic.Token.Column}",
                    diagnostic.Format());
            }

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

        private void lblLenguaje_Click(object sender, EventArgs e)
        {

        }
    }
}
