﻿using System;
using System.IO;
using System.Collections.Generic;
using CSharpRoslynAutoComplete;
using NDesk.Options;

namespace CSharpRoslynAutoCompleteClient
{
	class MainClass
	{
		static CSharpPrompter prompter = new CSharpPrompter();
		static int verbosity = 0;

		public static void Main(string[] args)
		{
			bool show_help = false;
			string currentParameter = string.Empty;
			bool useInteractiveMode = false;

			string code = string.Empty;
			int cursor = -1;
			List<string> assemblyPaths = new List<string>();

			// Build the arguments list
			var p = new OptionSet()
			{
				{ "p|program", 
					"the {PROGRAM} string to parse.", 
					v => {
						currentParameter = "p";
					}
				},
				{ "c|cursor", 
					"the current position of the {CURSOR} in the {PROGRAM} - " +
						"this must be an integer.",
					v => {
						currentParameter = "c";
					}
				},
				{ "d|dlls", 
					"the {ASSEMBLY} filepath lists to load with the {PROGRAM} - " +
						"this must be space separated paths.",
					v => currentParameter = "d"
				},
				{ "v|verbose", 
					"increase debug message verbosity",
					v => {
						if (v != null) ++verbosity; 
					} 
				},
				{
					"i|interactive",
					"run the program in interactive mode.",
					v => useInteractiveMode = v != null
				},
				{ "h|help",  
					"show this message and exit", 
					v => show_help = v != null
				},
				{ "<>", v => {
						switch (currentParameter)
						{
							case "d":
								assemblyPaths.Add(v);
								break;
							case "p":
								// Only parse the first string passed
								if (string.IsNullOrEmpty(code))
									code = v;
								break;
							case "c":
								// Only try to parse the first integer number passed
								if (cursor < 0)
								{
									if (int.TryParse(v, out cursor) == false)
									{
										cursor = -1;
									}
								}
								break;
						}
					}
				}
			};

			// Parse the arguments
			try
			{
				p.Parse(args);
			}
			catch (OptionException e)
			{
				Console.Write("CSharpRoslynAutoCompleteClient.exe: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `CSharpRoslynAutoCompleteClient.exe --help' for more information.");
				return;
			}

			if (show_help)
			{
				ShowHelp(p);
				return;
			}

			// Check for required arguments
			if (string.IsNullOrEmpty(code))
			{
				Console.WriteLine("CSharpRoslynAutoCompleteClient.exe: Missing required option -p|--program <PROGRAM>");
				Console.WriteLine("Try `CSharpRoslynAutoCompleteClient.exe --help' for more information.");
				return;
			}
			else if (cursor < 0)
			{
				Console.WriteLine("CSharpRoslynAutoCompleteClient.exe: Missing required option -c|--cursor <CURSOR> or error in parsing the argument.");
				Console.WriteLine("Try `CSharpRoslynAutoCompleteClient.exe --help' for more information.");
				return;
			}

			// Verbose output
			if (verbosity > 0 || useInteractiveMode)
			{
				PrintVerboseAdditionalAssembliesInfo(assemblyPaths);
				PrintVerboseProgramInfo(code, cursor);
				Console.WriteLine();
			}

			var suggestions = prompter.Prompt(code, cursor, assemblyPaths, verbosity);

			// Print any found suggestions
			foreach (var s in suggestions)
			{
				Console.WriteLine(s);
			}

			// Check for interactive mode
			if (useInteractiveMode)
			{
				Console.WriteLine();
				Console.WriteLine("Press the arrow keys LEFT and RIGHT to move the cursor, then ENTER to set it. ESC to quit.");

				// Handle CTRL+C gracefully
				Console.CancelKeyPress += new ConsoleCancelEventHandler((object sender, ConsoleCancelEventArgs ceargs) => {
					// Terminate on CTRL+C as on ESC
					ceargs.Cancel = false;

					Console.SetCursorPosition(0, Console.CursorTop + 3);
					Console.WriteLine("Bye!");
				});

				ConsoleKeyInfo cki;

				do
				{
					cki = Console.ReadKey(true);
					if (cki.Key == ConsoleKey.LeftArrow)
					{
						cursor--;
						if (cursor < 0)
						{
							cursor = 0;
						}
					}
					else if (cki.Key == ConsoleKey.RightArrow)
					{
						cursor++;
						if (cursor > code.Length - 1)
						{
							cursor = code.Length - 1;
						}
					}
						
					PrintVerboseProgramInfo(code, cursor);
					Console.SetCursorPosition(0, Console.CursorTop - 3);

					if (cki.Key != ConsoleKey.Enter)
					{
						continue;
					}

					Console.WriteLine();
					suggestions = prompter.Prompt(code, cursor, assemblyPaths, verbosity);

					// Print any found suggestions
					foreach (var s in suggestions)
					{
						Console.WriteLine(s);
					}

					Console.WriteLine();
					Console.WriteLine("Press the arrow keys LEFT and RIGHT to move the cursor, then ENTER to set it. ESC to quit.");
				}
				while (cki.Key != ConsoleKey.Escape);

				Console.SetCursorPosition(0, Console.CursorTop + 3);
				Console.WriteLine("Bye!");
			}
		}

		static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: CSharpRoslynAutoCompleteClient.exe");
			Console.WriteLine("Prompt code suggestions at current cursor position inside a C# program.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Error);
		}

		static void PrintVerboseProgramInfo(string code, int cursor)
		{
			Console.WriteLine(); 
			Console.WriteLine("PROGRAM"); 

			int currentChar = 0;
			foreach (Char c in code.ToCharArray())
			{
				if (currentChar == cursor)
				{
					Console.ForegroundColor = ConsoleColor.Green;
				}

				if (currentChar == cursor && Char.IsWhiteSpace(c))
				{
					Console.Write("_");
				}
				else
				{
					Console.Write(c);
				}

				if (currentChar == cursor)
				{
					Console.ResetColor();
				}

				currentChar++;
			}
			Console.WriteLine();
		}

		static void PrintVerboseAdditionalAssembliesInfo(List<string> assemblyPaths)
		{
			Console.WriteLine();
			Console.WriteLine("INCLUDED ASSEMBLIES:");
			assemblyPaths.ForEach(t => Console.WriteLine(t));
		}
	}
}
