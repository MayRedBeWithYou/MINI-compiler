
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
start			: Program spaces block Eof
				{					
					Compiler.ProgramTree.block = blockStack.Pop();
				}
				;
spaces			:
				| Endl {inc(); }
				;
block			: OpenBracket		
					{
						BlockNode node = new BlockNode();
						blockStack.Push(node);
						node.line = line;
						$$ = node;
					}
				  lines
				  CloseBracket
				;
lines			: 
				| instruction { $$ = $1; blockStack.Peek().lines.Add($1); } lines
				| Endl {inc();} lines
				;
instruction		: init Semicolon
				| assign Semicolon
				| write Semicolon
				| read Semicolon
				| exp Semicolon
				;
write			: Write String 
					{
						WriteNode node = new WriteNode(line);
						node.content = $2;
						$$ = node;
					}
				| Write exp 
					{
						WriteNode node = new WriteNode(line);
						node.content = $2;
						$$ = node;
					}
				;
read			: Read Variable
				;
init			: Int Variable 
					{
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "int";
						$$ = node;
					}
				| Double Variable 
					{
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "double";
						$$ = node;
					}
				| Bool Variable 
					{
						InitNode node = new InitNode();
						node.variable = (VariableNode)$2;
						node.variable.type = "bool";
						$$ = node;
					}
				;
assign			: Variable Assign exp 
					{
						AssignNode node = new AssignNode();
						node.left = (VariableNode)$1;
						node.right = $3;
						node.line = line;
						$$ = node;
					} 
				;
parExp			: OpenPar exp ClosePar 
					{
						ParenthesisNode node = new ParenthesisNode();
						node.content = $2;
						$$ = node;
					} 
				;

exp				: parExp
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
				| cast
				;
cast			: IntCast Variable
				| IntCast parExp 
				| DoubleCast Variable
				| DoubleCast parExp
				;
				
%%

public int line=1;

public ProgramNode root;

public Stack<BlockNode> blockStack = new Stack<BlockNode>();

public Node current;

public Parser(Scanner scanner) : base(scanner) { }

public void inc() { line++;}

public BinaryOpNode AssignToBinaryOp(BinaryOpNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	return node;
}

public ComparisonNode AssignToComparisonOp(ComparisonNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	return node;
}