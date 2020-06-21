
%using MINICompiler;
%namespace GardensPoint

%YYSTYPE Node

%right Assign
%left Or And
%left Comparison
%left Add Sub
%left Mult Div
%left BitOr BitAnd
%right Tilde Not IntCast DoubleCast

%token Program Write Read If Else While Return
%token OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Int Double Bool
%token Variable IntVal DoubleVal BoolVal String

%%
start			: Program block EOF
				  {
					if(syntaxErrorLines.Count != 0)
					{
						YYABORT;
					}
					ProgramTree.block = $2 as BlockNode;
					ProgramTree.Line = $1.Line;
				  }
				;
block			: OpenBracket
				  lines
				  CloseBracket
				  {
					BlockNode node;
					if($2 is null) node = new BlockNode();
					else node = new BlockNode($2 as BlockNode);
					node.Line = $1.Line;
					$$ = node;
				  }
				;
lines			: 
				| lines instruction
				{
					BlockNode node;
					if($1 is null) node = new BlockNode();
					else node = new BlockNode($1 as BlockNode);
					node.instructions.Add($2);
					$$ = node;
				}
				| EOF
				{			
					syntaxErrorLines.Add(ProgramTree.LineCount);
					YYABORT;
				}
				;
instruction		: init Semicolon
				| Return Semicolon
				| write Semicolon
				| read Semicolon
				| exp Semicolon
				{
					ExpressionNode node = $1 as ExpressionNode;
					node.ShouldReturnValue = false;
				}
				| if
				| while
				| block
				| Semicolon
				{
					syntaxErrorLines.Add($1.Line);
				}
				| error
				{
					syntaxErrorLines.Add($1.Line);
				}
				;

write			: Write String 
				{
					WriteNode node = new WriteNode($1.Line);
					node.content = $2;
					$$ = node;
				}
				| Write exp 
				{
					WriteNode node = new WriteNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				;
read			: Read Variable
				{
					ReadNode node = new ReadNode($1.Line);
					node.target = $2 as VariableNode;
					$$ = node;
				}
				;
init			: Int Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Int;
					$$ = node;

				}
				| Double Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Double;
					$$ = node;
					}
				| Bool Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Bool;
					$$ = node;
				}
				;
assign			: Variable Assign exp 
				{
					AssignNode node = new AssignNode($1.Line);
					node.left = $1 as VariableNode;
					node.right = $3;
					node.ShouldReturnValue = true;
					$$ = node;
				}
				;

exp				: OpenPar exp ClosePar 
				{
					ParenthesisNode node = new ParenthesisNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				} 
				| exp Add exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| exp Sub exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| exp Mult exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| exp Div exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| exp BitAnd exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| exp BitOr exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| Variable
				| IntCast exp
				{
					IntCastNode node = new IntCastNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| DoubleCast exp
				{
					DoubleCastNode node = new DoubleCastNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}				
				| IntVal
				| DoubleVal
				| BoolVal
				| Not exp
				{
					NotNode node = new NotNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| Tilde exp
				{
					NegNode node = new NegNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| Sub exp
				{
					MinusNode node = new MinusNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| exp And exp
				{
					LogicOpNode node = $2 as LogicOpNode;
					node.left = $1 as ExpressionNode;
					node.right = $3 as ExpressionNode;
					$$ = node;
				}
				| exp Or exp
				{
					LogicOpNode node = $2 as LogicOpNode;
					node.left = $1 as ExpressionNode;
					node.right = $3 as ExpressionNode;
					$$ = node;
				}
				| exp Comparison exp 
				{
					ComparisonNode node = $2 as ComparisonNode;
					$$ = AssignToComparisonOp(node, $1 as ExpressionNode, $3 as ExpressionNode);
				}
				| assign
				;

if				: If OpenPar exp ClosePar instruction
				{
					IfNode node = new IfNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.ifBlock = $5;
					$$ = node;
				}
				| If OpenPar exp ClosePar instruction Else instruction
				{
					IfNode node = new IfNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.elseBlock = $7;
					node.ifBlock = $5;
					$$ = node;
				}				
				;
while			: While OpenPar exp ClosePar instruction
				{
					WhileNode node = new WhileNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.block = $5;
					$$ = node;
				}
				;
				
%%

public ProgramNode ProgramTree;

public HashSet<int> syntaxErrorLines = new HashSet<int>();

public Parser(Scanner scanner, ProgramNode node) : base(scanner)
{
	ProgramTree = node;
}

public BinaryOpNode AssignToBinaryOp(BinaryOpNode node, ExpressionNode left, ExpressionNode right)
{
	node.left = left;
	node.right = right;
	return node;
}

public ComparisonNode AssignToComparisonOp(ComparisonNode node, ExpressionNode left, ExpressionNode right)
{
	node.left = left;
	node.right = right;
	return node;
}