namespace I.Do.Brief
{
    // this is a stripped down Brief engine used as a protocol and interactive
    // commands via TCP/UDP/HTTP/SQS/... are Brief words

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class Word
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string Type;
        public readonly Func<Context, Context> Function;

        private readonly Func<string> toString;

        public Word(string name, string description, string type, Func<Context, Context> function, Func<string> toString)
        {
            this.Name = name;
            this.Description = description;
            this.Type = type;
            this.Function = function;
            this.toString = toString;
        }

        public Word(string name, string description, string type, Func<Context, Context> function)
            : this(name, description, type, function, () => name)
        {
        }

        public override string ToString()
        {
            return this.toString();
        }
    }

    public class Context
    {
        public Dictionary<string, dynamic> Storage = new Dictionary<string, dynamic>();

        public Dictionary<string, Word> Dictionary = new Dictionary<string, Word>();

        public Stack<dynamic> Stack = new Stack<dynamic>();

        public void Push(dynamic value)
        {
            this.Stack.Push(value);
        }

        public dynamic Pop()
        {
            return this.Stack.Pop();
        }

        public void AddWord(string name, string description, Func<Context, Context> fn)
        {
            this.Dictionary.Add(name, new Word(name, description, "word", fn));
        }

        public void AddWord00(string name, string description, Action fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    fn();
                    return c;
                });
        }

        public void AddWord10(string name, string description, Action<dynamic> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    fn(c.Pop());
                    return c;
                });
        }

        public void AddWord20(string name, string description, Action<dynamic, dynamic> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    var a = c.Pop();
                    var b = c.Pop();
                    fn(a, b);
                    return c;
                });
        }

        public void AddWord11(string name, string description, Func<dynamic, dynamic> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    c.Push(fn(c.Pop()));
                    return c;
                });
        }

        public void AddWord12(string name, string description, Func<dynamic, Tuple<dynamic, dynamic>> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    var t = fn(c.Pop());
                    c.Push(t.Item1);
                    c.Push(t.Item2);
                    return c;
                });
        }

        public void AddWord21(string name, string description, Func<dynamic, dynamic, dynamic> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    var a = c.Pop();
                    var b = c.Pop();
                    c.Push(fn(a, b));
                    return c;
                });
        }

        public void AddWord22(string name, string description, Func<dynamic, dynamic, Tuple<dynamic, dynamic>> fn)
        {
            this.AddWord(
                name,
                description,
                c =>
                {
                    var a = c.Pop();
                    var b = c.Pop();
                    var t = fn(a, b);
                    c.Push(t.Item1);
                    c.Push(t.Item2);
                    return c;
                });
        }
    }

    public static class Brief
    {
        public static IEnumerable<Word> Parse(string source, Dictionary<string, Word> dictionary)
        {
            return Parse(Lex(source), dictionary);
        }

        public static string Print(IEnumerable<Word> code)
        {
            var sb = new StringBuilder();
            foreach (var word in code)
            {
                sb.Append(word);
                sb.Append(' ');
            }

            return sb.ToString();
        }

        private static IEnumerable<string> Lex(string source)
        {
            // strings are delimited by "..." while lists are delimeted by [...]. these become tokens during lexing
            // otherwise, tokens are merely whitespace separated (note, `Split(null)` assumes *any* whitespace character)
            // a known issue is that leading/trailing space in string literals is lost - fine, we'll say that's the semantics
            return source.Replace("[", " [ ").Replace("]", " ] ").Replace("\"", " \" ").Split(null).Where(w => w.Length > 0).Reverse();
        }

        private static Word ParseToken(string token, Dictionary<string, Word> dictionary)
        {
            bool b;
            if (bool.TryParse(token, out b))
            {
                return new Word(token, "Boolean literal", "bool", c => { c.Push(b); return c; });
            }

            int i;
            if (int.TryParse(token, out i))
            {
                return new Word(token, "Integer literal", "int", c => { c.Push(i); return c; });
            }

            double d;
            if (double.TryParse(token, out d))
            {
                return new Word(token, "Double literal", "double", c => { c.Push(d); return c; });
            }

            DateTime dt;
            if (DateTime.TryParse(token, out dt))
            {
                return new Word(token, "DateTime literal", "datetime", c => { c.Push(dt); return c; }, () => dt.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            }

            if (token[0] == '\'')
            {
                token = token.Substring(1);
                return new Word(token, "Symbolic string literal", "string", c => { c.Push(token); return c; });
            }

            if (dictionary.ContainsKey(token))
            {
                var word = dictionary[token];
                return new Word(token, "Defined word", "word", word.Function);
            }

            throw new Exception($"Unknown word: '{token}'");
        }

        private static string PrettyPrintList(LinkedList<Word> list)
        {
            // print [<val0> <val1> <val2> ...] or fewer values
            var head = list.First;
            var sb = new StringBuilder();
            sb.Append('[');
            for (var i = 0; i < 3 && head != null; i++) // first n-elements max
            {
                sb.Append(head.Value);
                sb.Append(' ');
                head = head.Next;
            }

            if (head != null)
            {
                sb.Append("... "); // elipsis if more elements
            }

            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - 1, 1); // trailing space
            }

            sb.Append(']');

            return sb.ToString();
        }

        private static Tuple<Word, LinkedListNode<string>> ParseString(LinkedListNode<string> head)
        {
            // assemble tokens up to terminating " as a space-separated string
            // no escape characters are supported
            // no leading/trailing or multiple spaces are preserved
            var tokens = new Stack<string>();
            while (head != null)
            {
                if (head.Value == "\"")
                {
                    var sb = new StringBuilder();
                    foreach (var t in tokens)
                    {
                        sb.Append(t);
                        sb.Append(' ');
                    }
                    sb.Remove(sb.Length - 1, 1); // trailing space
                    var str = sb.ToString();
                    return Tuple.Create(new Word("\"...\"", "String literal", "string", c => { c.Push(str); return c; }, () => $"\"{str}\""), head.Next);
                }
                else
                {
                    tokens.Push(head.Value);
                }

                head = head.Next;
            }

            throw new Exception("Expected ], but found END.");
        }

        private static Tuple<Word, LinkedListNode<string>> ParseList(LinkedListNode<string> head, Dictionary<string, Word> dictionary)
        {
            // parse list of words to terminating ]
            // called to parse each line with terminator added
            var list = new LinkedList<Word>();
            while (head != null)
            {
                switch (head.Value)
                {
                    case "]":
                        var parsedList = ParseList(head.Next, dictionary);
                        list.AddLast(parsedList.Item1);
                        head = parsedList.Item2;
                        break;
                    case "[":
                        return Tuple.Create(new Word("[...]", "List literal", "list", s => { s.Push(list); return s; }, () => PrettyPrintList(list)), head.Next);
                    case "\"":
                        var parsedString = ParseString(head.Next);
                        list.AddLast(parsedString.Item1);
                        head = parsedString.Item2;
                        break;
                    default:
                        list.AddLast(ParseToken(head.Value, dictionary));
                        head = head.Next;
                        break;
                }
            }

            throw new Exception("Expected ], but found END.");
        }

        private static IEnumerable<Word> Parse(IEnumerable<string> tokens, Dictionary<string, Word> dictionary)
        {
            // here a flat sequence of tokens is given structure (lists, strings) and literal types
            var list = new LinkedList<string>(tokens);
            list.AddLast("[");
            var parsed = ParseList(list.First, dictionary);
            if (parsed.Item2 != null)
            {
                throw new Exception($"Malformed expression. Expected END, but found: {parsed.Item2.Value}");
            }

            var ctx = parsed.Item1.Function(new Context());
            return ctx.Pop();
        }
    }

    public class Machine
    {
        private bool trace = false;

        public Context Context { get; private set; }

        public Machine()
        {
            this.Context = new Context();
            this.Context.AddWord10("trace", "Enable/disable debug tracing (`trace true`, `trace false`, `trace <bool>`)", b => this.trace = b);
            this.Context.AddWord10("print", "Print message to console (`print 'hello`, `print \"this is a test\"`)", s => Console.WriteLine(s));
            this.Context.AddWord21("+", "Addition (`+ 3 4`, `+ <int/double> <int/double>`)", (a, b) => a + b);
            this.Context.AddWord21("-", "Subtraction (`- 3 4`, `- <int/double> <int/double>`)", (a, b) => a - b);
            this.Context.AddWord21("*", "Multiplication (`* 3 4`, `* <int/double> <int/double>`)", (a, b) => a * b);
            this.Context.AddWord21("/", "Division (`/ 3 4`, `/ <int/double> <int/double>`)", (a, b) => a / b);
            this.Context.AddWord21("mod", "Modulus (`mod 3 4`, `mod <int/double> <int/double>`)", (a, b) => a / b);
            this.Context.AddWord22("swap", "Swap top two stack elements (`swap`)", (a, b) => new Tuple<dynamic, dynamic>(b, a));
            this.Context.AddWord12("dup", "Duplicate top stack element (`dup`)", a => new Tuple<dynamic, dynamic>(a, a));
            this.Context.AddWord10("drop", "Drop top stack element (`drop`)", _ => { });
            this.Context.AddWord(".", "Display top of stack (`.`)", c => { Console.WriteLine(c.Stack.Peek()); return c; });
            this.Context.AddWord(".s", "Display stack (`.s`)", c => { DisplayStack(c.Stack); return c; });
            this.Context.AddWord("clear", "Clear stack values (`clear`)", c => { c.Stack.Clear(); return c; });
            this.Context.AddWord21("=", "Equality comparision (`= x y`)", (a, b) => a == b);
            this.Context.AddWord21(">", "Greater than comparision (`> x y`)", (a, b) => a > b);
            this.Context.AddWord21(">=", "Greater than or equal comparision (`>= x y`)", (a, b) => a >= b);
            this.Context.AddWord21("<", "Less than comparision (`< x y`)", (a, b) => a < b);
            this.Context.AddWord21("<=", "Less than or equal comparision (`<= x y`)", (a, b) => a <= b);
            this.Context.AddWord21("and", "Boolean conjunction (`and x y`)", (a, b) => a && b);
            this.Context.AddWord21("or", "Boolean disjunction (`or x y`)", (a, b) => a || b);
            this.Context.AddWord11("not", "Negation (`not x y`)", a => !a);
            this.Context.AddWord("if", "Conditionally execute a quotation (`if [<when true>] [<when false>] <predicate>`)", c =>
            {
                LinkedList<Word> t = c.Pop();
                LinkedList<Word> f = c.Pop();
                var p = c.Pop();
                return Execute(p ? t : f, c);
            });
            this.Context.AddWord20("def", "Define secondary word (`def 'myword [<my code>]`)", (name, quotation) =>
            {
                Func<Context, Context> fn = c => Execute(quotation, c);
                this.Context.AddWord(name, Brief.Print(quotation), fn);
            });
            this.Context.AddWord10("load", "Load source from file (`load 'foo.b`, `load <file>`)", path =>
            {
                foreach (var line in ((string[])File.ReadAllLines(path)))
                {
                    if (line.Length > 0 && !line.StartsWith("\\ ")) this.Execute(line);
                }
            });
            this.Context.AddWord00("words", "Lists words available in the current dictionary", () =>
            {
                foreach (var w in this.Context.Dictionary.Values)
                {
                    Console.WriteLine($"`{w.Name}` - {w.Description}");
                }
            });
            this.Context.AddWord10("help", "Help with a particular word (`help dup`)", n =>
            {
                Word w = this.Context.Dictionary[n];
                Console.WriteLine($"Word: {w.Name}\nDescription: {w.Description}\nType: {w.Type}");
            });
        }

        private void DisplayStack(Stack<dynamic> stack)
        {
            foreach (var value in stack)
            {
                Console.Write($"{value} ");
            }
            Console.WriteLine();
        }

        Context Execute(IEnumerable<Word> code, Context context)
        {
            if (this.trace)
            {
                Console.WriteLine($"Execute: {Brief.Print(code)}");
            }

            foreach (var word in code)
            {
                context = word.Function(context);
                if (this.trace)
                {
                    Console.WriteLine($"{word} -> {(context.Stack.Count == 0 ? "<empty>" : context.Stack.Peek())}");
                }
            }

            return context;
        }

        private object protect = new object();

        public void Execute(string source)
        {
            lock (protect)
            {
                this.Context = this.Execute(Brief.Parse(source, this.Context.Dictionary), this.Context);
            }
        }

        public static void ReadEvalPrintLoop(string name, Machine machine)
        {
            Console.WriteLine($"Welcome to {name}, powered by Brief");
            Console.WriteLine();
            Console.WriteLine("Type `words` for available commands or `help '<name>` specific words. `exit` to exit.");
            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == "exit") break;
                try
                {
                    machine.Execute(line);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}