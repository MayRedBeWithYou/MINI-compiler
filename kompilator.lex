
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

"="			{ return (int)Tokens.Assign; }

"+"			{ return (int)Tokens.Add; }
"-"			{ return (int)Tokens.Sub; }
"*"			{ return (int)Tokens.Mult; }
"/"			{ return (int)Tokens.Div; }

"&"			{ return (int)Tokens.BitAnd; }
"&&"		{ return (int)Tokens.And; }
"|"			{ return (int)Tokens.BitOr; }
"||"		{ return (int)Tokens.Or; }

"=="		{ return (int)Tokens.Comparison; }
"!="		{ return (int)Tokens.Comparison; }
"<"			{ return (int)Tokens.Comparison; }
"<="		{ return (int)Tokens.Comparison; }
">"			{ return (int)Tokens.Comparison; }
">="		{ return (int)Tokens.Comparison; }
"!"			{ return (int)Tokens.Not;}
"~"			{ return (int)Tokens.Tilde;}
"(int)"		{ return (int)Tokens.IntCast;}
"(double)"	{ return (int)Tokens.DoubleCast;}

";"			{ return (int)Tokens.Semicolon; }
"\n"		{ return (int)Tokens.Endl; }
" "         { }
"\t"        { }
"{"			{ return (int)Tokens.OpenBracket;}
"}"			{ return (int)Tokens.CloseBracket;}

{DoubleVal}	{ yylval.node = new DoubleNode(double.Parse(yytext)); return (int)Tokens.DoubleVal; }
{IntVal}	{ yylval.node = new IntNode(int.Parse(yytext)); return (int)Tokens.IntVal; }
{BoolVal}	{ yylval.node = new BoolNode(bool.Parse(yytext)); return (int)Tokens.BoolVal; }
{String}	{ yylval.node = new StringNode(yytext); return (int)Tokens.String; }
{Ident}		{ yylval.node = new VariableNode(yytext); return (int)Tokens.Variable; }

<<EOF>>		{ return (int)Tokens.Eof; }

%%