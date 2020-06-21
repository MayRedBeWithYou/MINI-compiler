
%using MINICompiler;
%namespace GardensPoint

DoubleVal	(0|((-?)[1-9][0-9]*))+\.([0-9])+
IntVal		0|(-?)[1-9]([0-9])*
BoolVal		true|false
String		\"([^\\\"\n]|\\.)*\"
Ident		[a-zA-Z][a-zA-Z0-9]*
IntCast		\([ \t]*int[ \t]*\)
DoubleCast	\([ \t]*double[ \t]*\)
Comment		\/\/([^\n]|\\.)*

%%
"program"		{ ProgramTree.Line = yyline; yylval = new EmptyNode(yyline); return (int)Tokens.Program; }
"return"		{ yylval = new ReturnNode(yyline); return (int)Tokens.Return;}

"write"			{ yylval = new EmptyNode(yyline); return (int)Tokens.Write; }
"read"			{ yylval = new EmptyNode(yyline); return (int)Tokens.Read; }

"int"			{ yylval = new EmptyNode(yyline); return (int)Tokens.Int; }
"double"		{ yylval = new EmptyNode(yyline); return (int)Tokens.Double; }
"bool"			{ yylval = new EmptyNode(yyline); return (int)Tokens.Bool; }

"&"				{ yylval = new BinaryOpNode(BinaryOpType.BitAnd, yyline); return (int)Tokens.BitAnd; }
"&&"			{ yylval = new LogicOpNode(LogicOpType.And, yyline); return (int)Tokens.And; }
"|"				{ yylval = new BinaryOpNode(BinaryOpType.BitOr, yyline); return (int)Tokens.BitOr; }
"||"			{ yylval = new LogicOpNode(LogicOpType.Or, yyline); return (int)Tokens.Or; }

"=="			{ yylval = new ComparisonNode(ComparisonType.Equal, yyline); return (int)Tokens.Comparison; }
"!="			{ yylval = new ComparisonNode(ComparisonType.NotEqual, yyline); return (int)Tokens.Comparison; }
"<"				{ yylval = new ComparisonNode(ComparisonType.Less, yyline); return (int)Tokens.Comparison; }
"<="			{ yylval = new ComparisonNode(ComparisonType.LessOrEqual, yyline); return (int)Tokens.Comparison; }
">"				{ yylval = new ComparisonNode(ComparisonType.Greater, yyline); return (int)Tokens.Comparison; }
">="			{ yylval = new ComparisonNode(ComparisonType.GreaterOrEqual, yyline); return (int)Tokens.Comparison; }
"!"				{ yylval = new EmptyNode(yyline); return (int)Tokens.Not;}
"~"				{ yylval = new EmptyNode(yyline); return (int)Tokens.Tilde;}
{IntCast}		{ yylval = new EmptyNode(yyline); return (int)Tokens.IntCast;}
{DoubleCast}	{ yylval = new EmptyNode(yyline); return (int)Tokens.DoubleCast;}

"="				{ yylval = new EmptyNode(yyline); return (int)Tokens.Assign; }

"+"				{ yylval = new BinaryOpNode(BinaryOpType.Add, yyline); return (int)Tokens.Add; }
"-"				{ yylval = new BinaryOpNode(BinaryOpType.Sub, yyline); return (int)Tokens.Sub; }
"*"				{ yylval = new BinaryOpNode(BinaryOpType.Mult, yyline); return (int)Tokens.Mult; }
"/"				{ yylval = new BinaryOpNode(BinaryOpType.Div, yyline); return (int)Tokens.Div; }
	
"("				{ yylval = new EmptyNode(yyline); return (int)Tokens.OpenPar;}
")"				{ yylval = new EmptyNode(yyline); return (int)Tokens.ClosePar;}
";"				{ yylval = new EmptyNode(yyline); return (int)Tokens.Semicolon; }
"\n"			{ ProgramTree.LineCount++; }
"\r"			{ ProgramTree.LineCount++; }
" "			    { }
"\t"			{ }
{Comment}		{ }
"{"				{ yylval = new EmptyNode(yyline); return (int)Tokens.OpenBracket;}
"}"				{ yylval = new EmptyNode(yyline); return (int)Tokens.CloseBracket;}
"if"			{ yylval = new EmptyNode(yyline); return (int)Tokens.If;}
"else"			{ yylval = new EmptyNode(yyline); return (int)Tokens.Else;}
"while"			{ yylval = new EmptyNode(yyline); return (int)Tokens.While;}

{DoubleVal}		{ yylval = new DoubleNode(double.Parse(yytext), yyline); return (int)Tokens.DoubleVal; }
{IntVal}		{ yylval = new IntNode(int.Parse(yytext), yyline); return (int)Tokens.IntVal; }
{BoolVal}		{ yylval = new BoolNode(bool.Parse(yytext), yyline); return (int)Tokens.BoolVal; }
{String}		{ yylval = new StringNode(yytext, yyline); return (int)Tokens.String; }
{Ident}			{ yylval = new VariableNode(yytext, yyline); return (int)Tokens.Variable; }

.				{ return (int)Tokens.error;}
%%

ProgramNode ProgramTree;

public Scanner(FileStream stream, ProgramNode tree) : this(stream)
{
	ProgramTree = tree;
}

public int Line => yyline;