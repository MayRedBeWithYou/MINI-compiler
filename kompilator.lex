
%using MINICompiler;
%namespace GardensPoint

IntNum		0|[1-9]([0-9])*
DoubleNum	(0|[1-9]([0-9])*)+\.([0-9])+
Bool		true|false|TRUE|FALSE
String		\"[a-zA-Z0-9]*\"
Ident		[a-zA-Z][a-zA-Z0-9]*

%%
{IntNum}	{ yylval.node = new IntNode(int.Parse(yytext)); return (int)Tokens.Int;}
{DoubleNum}	{ yylval.node = new DoubleNode(double.Parse(yytext)); return (int)Tokens.Double;}
{String}	{yylval.node = new StringNode(yytext); return (int)Tokens.String;}
{Bool}		{yylval.node = new BoolNode(bool.Parse(yytext)); return (int)Tokens.Bool;}
";"			{ return (int)Tokens.Semicolon; }
"\n"		{ return (int)Tokens.Endl; }
" "           { }
"\t"          { }
"program"	{ return (int)Tokens.Program; }
"{"			{ return (int)Tokens.OpenBracket;}
"}"			{ return (int)Tokens.CloseBracket;}
<<EOF>>		{ return (int)Tokens.Eof; }
"write"		{ return (int)Tokens.Write; }
%%