
%using MINICompiler;
%namespace GardensPoint

%YYSTYPE Node

%right Assign
%left And Or
%left Comparison
%left Add Sub
%left Mult Div
%left BitOr BitAnd
%right Minus Tilde Not IntCast DoubleCast

%token Program Write Read If Else While Return
%token Endl OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Eof Error
%token Int Double Bool
%token Variable IntVal DoubleVal BoolVal String

%%
start			: Program spaces 
					{
						root = new ProgramNode();
						current = root;
						root.line = line;
						list.Add(root);						
					}
					block
					Eof {Console.WriteLine("End of file. Lines: " + line);}
				;
spaces			:
				| Endl {inc(); } spaces
				;
block			: OpenBracket spaces 
					{
						Console.WriteLine("Opening block."); 
						BlockNode node = new BlockNode(); 
						node.line = line; 
						list.Add(node);
					}
				  lines
				  CloseBracket spaces 
					{
						Console.WriteLine("Closing block.");
					}
				;
lines			: { Console.WriteLine("No more lines"); }
				| line lines
				| Endl {inc();} lines
				;
line			: init Semicolon
				| assign Semicolon
				| write Semicolon
				| exp Semicolon
				;
write			: Write String 
					{
						WriteNode node = new WriteNode(line);
						node.content = $2;
						list.Add(node);
					}
				| Write exp 
					{
						WriteNode node = new WriteNode(line);
						list.Add(node);
					}
				;
init			: Int Variable 
					{
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "int";
						list.Add(node);
					}
				| Double Variable 
					{
						Console.WriteLine("Found double init.");
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "double";
						list.Add(node);
					}
				| Bool Variable 
					{
						Console.WriteLine("Found bool init.");
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "bool";
						 list.Add(node);
					}
				;
assign			: Variable Assign exp 
					{
						Console.WriteLine("Found assignment.");
						AssignNode node = new AssignNode();
						node.left = (VariableNode)$1;
						node.right = $3;
						node.line = line;
						list.Add(node);
						$$ = node;
					} 
				;
exp				: OpenPar exp ClosePar 
					{
						ParenthesisNode node = new ParenthesisNode();
						node.content = $2;
						list.Add(node);
						$$ = node;
					} 
				| exp Add exp
					{
						BinaryOpNode node = $2 as BinaryOpNode;
						$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
					}
				| exp Sub exp
					{
						BinaryOpNode node = $2 as BinaryOpNode;
						$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
					}
				| exp Mult exp 
					{
						BinaryOpNode node = $2 as BinaryOpNode;
						$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
					}
				| exp Div exp 
					{
						BinaryOpNode node = $2 as BinaryOpNode;
						$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
					}
				| exp Comparison exp 
					{
						ComparisonNode node = $2 as ComparisonNode;
						$$ = AssignToComparisonOp(node, $1 as Node, $3 as Node); 
					}
				| Variable
				| IntVal
				| DoubleVal
				| BoolVal
				;
cast			: IntCast Variable
				| IntCast exp
				| DoubleCast Variable
				| DoubleCast exp
				;
				
%%

public int line=1;

public ProgramNode root;

public List<Node> list = new List<Node>();

public Node current;

public Parser(Scanner scanner) : base(scanner) { }

public void inc() { line++;}

public BinaryOpNode AssignToBinaryOp(BinaryOpNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	list.Add(node);
	return node;
}

public ComparisonNode AssignToComparisonOp(ComparisonNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	list.Add(node);
	return node;
}