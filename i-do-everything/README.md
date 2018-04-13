# Main i-do App

This is a classic .NET app (for speech reco/synth) hosting a Brief language engine as the "glue" integrating many things.

## Brief Words

Type `words` at the REPL for a complete list, but here are some important i-do words:

* *Speech*
    * `say` - Speak given string
    * `phrase` - Add phrase to speech recognition grammar; bind to Brief expression (`phrase 'hello [say "hi there"])
    * `reco` - Start speech recognition, after having added `phrase` bindings (`reco`)
* *Windows*
    * `window` - Show window in foreground by process name (`window 'skype)
    * `key` - Send key to forground app (`key '^{q})

## Brief Language

The Brief language is extreemely simple. There is almost no syntax and then semantic are simple (`context -> context`) transformation functions.

## Syntax

Tokens are generally whitespace separated. One exception is string literals (`"such as this"`). The other exception is list syntax in which `[` and `]` are themselves delimiter tokens.

Tokens represent either literal values such as integers (`123`), doubles (`2.718`), booleans (`true`/`false`), datetimes (`2017-03-23T21:03:44.123`) or strings (`"such as this"` or `'this`). Datetimes may be anything that can be parsed by `DateTime.TryParse(...)` but must not contain whitespace or else will be considered separate tokens. Strings may contain whitespace when delimited by double quotes (`"..."`). A simple leading single quote (`'`) also indicates a string (without whitespace). This is often used for simple symbolic names (`'foo`).

Tokens that cannot be parsed as an integer, double, boolean, datetime or string are considered "words" that represent transformation functions in the language. To see a list of understood words in the system use the `words` word.

Lists are the single means of combination in the language. Any sequence of literals and/or words may be contained within square brackets (`[...]`). Lists may be nested.

## Semantics

Every token in Brief represents a `context -> context` function. The context is comprised of a `Stack<dynamic>` used for parameter passing and return values, a storage `Dictionary<string, dynamic>` and a mapping of names to words: `Dictionary<string, Word>`. Tokens are processed from right to left. This makes them prefix operators (as opposed to postfix or infix). Since the arity (number of stack values consumed/returned) is fixed, there is no need for parenthesis, but Brief _can_ be thought of as a Lisp without parenthesis; prefix functions. Another way to think of it is as a Forth, but rather than _reverse_ Polish notations it's just Polish notation - prefix, not postfix, but fixed arity. Internally in fact, Brief is a Forth, just processed right-to-left making the syntax prefix, but the semantics are "reversed postfix" if that makes sense :)

Even literal values become words during parsing. They are words that have the effect of pushing a value to the stack. Operations then consume these values as arguments and push back return values. Literals can be though of as taking zero arguments and returning one value (a 0-1 arity). Arithematic operations, for example, take two arguments and return one (a 2-1 arity). An example expression:

    + 3 * 4 5

This expression is processed right-to-left. `5` pushes this integer to the stack. `4` does as well. The multiplication word (`*`) pops these two values and pushes back their product (`20` is on the stack now). `3` pushes and `+` pops two values and pushes their sum (`23` results on the stack).

Again, if you're familiar with Lisp, you can think of this as `(+ 3 (* 4 5))` but the parenthesis are not needed. If you're familiar with Forth then it is merely reversed RPN (reversed Polish notation): `4 5 * 3 +`.

Lists are pushed to the stack as well as literal values to be processed by words. These lists may contain zero or more simple values or code. When they contain code we call them "quotations" and can be thought of as anonymous lambdas. This is use with conditional words such as `if` (as in `if [do this] [otherwise do that]`). Conditional expressions take their predicate from the stack and so are usually followed by a comparision or similar expression (using `>`, `<`, `=`, `and`, `or`, `not`, ... words).

The system comes with a set of primitive words already defined in the dictionary. A domain specific implimentation may add others bound to .NET functions. Additionally, you are free to define secondary words in terms of these primitives (or other secondaries). This is done with the `def` word, which takes a name and a quotation from the stack.

    > def 'square [* dup]

Notice that the `dup` word will expect a value on the stack which is not provided in the quotation. This is how new words taking parameters may be defined. The top stack value is duplicated and then multiplied (`*`) by itself to `square` it. This new word will show up in the `words` list and may then be used:

    > square 5
    > .
    25

## System

A Brief system is just a console application that takes code in the above format from command line arguments or standard in (human at a REPL or piped). The `load` word will allow loading source from files. In this way, the language serves as a REPL, a CLI arg format and as the configuration file format.

The system does provide primitives for basic arithmetic, comparision, conditionals and self-inspection, but the real purpose is to be hosted and provided domain specific words. In the case of `PsiRemoting.exe` these are words related to exploring stores and importing/exporting streams. The system is very easily extended with more words.

To host then system:

    var machine = new Machine();

To add DSL words with a name and description of your choosing, bound to simple `context -> context` functions:

    machine.Context.AddWord("howdy", "Greet given name", c => { var name = c.Pop(); c.Push($"Hello, {name}"); });

Or use one of the arity-specific overloads (e.g. 1-1 in this case):

    machine.Context.AddWord11("howdy", "Greet given name", name => $"Hello, {name}");

Each arity overload takes a `Func<dynamic, ...>` or `Action<dynamic, ...>` or, in the case of taking or returning multiple values, a `Func<Tuple<dynamic, ...>, Tuple<dynamic, ...>>`, etc. This makes it *very* easy to bind existing functions to Brief words.

### Source Files

A note about source files. In keeping with the prefix notation, source files are also processed in reverse. That is, bottom-to-top. The common idiom is to indent and define word _below_ words which depend on the definitions. For example:

    def 'area [* pi square]
        def 'pi [3.14159265]
        def 'square [* dup]

This is read left-to-right, top-to-bottom as, "`area` is the product (`*`) of `pi` and the `square` of something, _where_ `pi` is `3.14159265` and `square` is the product of something `dup`licated. Code is written top-down, but processed bottom-up, literally.
