
%using MINICompiler;
%namespace GardensPoint

DoubleVal	(0|((-?)[1-9][0-9]*))+\.([0-9])+
IntVal		0|(-?)[1-9]([0-9])*
BoolVal		true|false
String		\"([^\\\"\n]|\\.)*\"
Ident		[a-zA-Z][a-zA-Z0-9]*

%%
"program"	{ return (int)Tokens.Program; }

"write"		{ return (int)Tokens.Write; }
"read"		{ return (int)Tokens.Read; }

"int"		{ return (int)Tokens.Int; }
"double"	{ return (int)Tokens.Double; }
"bool"		{ return (int)Tokens.Bool; }

"&"			{ yylval = new BinaryOpNode(BinaryOpType.BitAnd); return (int)Tokens.BitAnd; }
"&&"		{ yylval = new LogicOpNode(LogicOpType.And); return (int)Tokens.And; }
"|"			{ yylval = new BinaryOpNode(BinaryOpType.BitOr); return (int)Tokens.BitOr; }
"||"		{ yylval = new LogicOpNode(LogicOpType.Or); return (int)Tokens.Or; }

"=="		{ yylval = new ComparisonNode(ComparisonType.Equal); return (int)Tokens.Comparison; }
"!="		{ yylval = new ComparisonNode(ComparisonType.NotEqual); return (int)Tokens.Comparison; }
"<"			{ yylval = new ComparisonNode(ComparisonType.Less); return (int)Tokens.Comparison; }
"<="		{ yylval = new ComparisonNode(ComparisonType.LessOrEqual); return (int)Tokens.Comparison; }
">"			{ yylval = new ComparisonNode(ComparisonType.Greater); return (int)Tokens.Comparison; }
">="		{ yylval = new ComparisonNode(ComparisonType.GreaterOrEqual); return (int)Tokens.Comparison; }
"!"			{ return (int)Tokens.Not;}
"~"			{ return (int)Tokens.Tilde;}
"(int)"		{ return (int)Tokens.IntCast;}
"(double)"	{ return (int)Tokens.DoubleCast;}

"="			{ return (int)Tokens.Assign; }

"+"			{ yylval = new BinaryOpNode(BinaryOpType.Add); return (int)Tokens.Add; }
"-"			{ yylval = new BinaryOpNode(BinaryOpType.Sub); return (int)Tokens.Sub; }
"*"			{ yylval = new BinaryOpNode(BinaryOpType.Mult); return (int)Tokens.Mult; }
"/"			{ yylval = new BinaryOpNode(BinaryOpType.Div); return (int)Tokens.Div; }

"("			{ return (int)Tokens.OpenPar;}
")"			{ return (int)Tokens.ClosePar;}
";"			{ return (int)Tokens.Semicolon; }
"\n"		{ ProgramTree.lineCount++; }
" "         { }
"\t"        { }
"{"			{ return (int)Tokens.OpenBracket;}
"}"			{ return (int)Tokens.CloseBracket;}
"if"		{ return (int)Tokens.If;}
"else"		{ return (int)Tokens.Else;}
"while"		{ return (int)Tokens.While;}

{DoubleVal}	{ yylval = new DoubleNode(double.Parse(yytext), ProgramTree.lineCount); return (int)Tokens.DoubleVal; }
{IntVal}	{ yylval = new IntNode(int.Parse(yytext), ProgramTree.lineCount); return (int)Tokens.IntVal; }
{BoolVal}	{ yylval = new BoolNode(bool.Parse(yytext), ProgramTree.lineCount); return (int)Tokens.BoolVal; }
{String}	{ yylval = new StringNode(yytext, ProgramTree.lineCount); return (int)Tokens.String; }
{Ident}		{ yylval = new VariableNode(yytext, ProgramTree.lineCount); return (int)Tokens.Variable; }

<<EOF>>		{ return (int)Tokens.Eof; }

%%

ProgramNode ProgramTree;

public Scanner(FileStream stream, ProgramNode tree) : this(stream)
{
	ProgramTree = tree;
}