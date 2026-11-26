using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// Notes every Function up front, since a call may come before the definition, and checks each one's first line.
    /// They go to the current scope's Functions: the text being compiled, or a Library being loaded.
    /// </summary>
    private void CollectFunctions(List<Statement> statements)
    {
        var known = Functions;
        var functions = statements.OfType<FunctionDefinition>().ToList();
        foreach (var function in functions)
        {
            var name = function.Name;
            if (CheckName(name) && !known.ContainsKey(name.Name))
            {
                known.Add(name.Name, function);

                // Before any Setting inputs are found, since finding them follows calls within this Library
                if (_scope.Library != null)
                    _libraryFunctions.Add(function, _scope.Library);
            }
            else if (known.TryGetValue(name.Name, out var first))
                Error(name, $"Function '{name.Name}' is already defined on line {first.Position.Line}");
        }

        foreach (var function in functions)
        {
            if (known.TryGetValue(function.Name.Name, out var registered) && registered == function && !CheckHeader(function))
                _brokenFunctions.Add(function);
        }

        // A Library's Functions are checked for Setting input uses once it's loaded, which may be during a search for
        // its Setting inputs
        if (_scope.Library != null)
            return;

        foreach (var function in known.Values)
        {
            CheckSettingInputUses(function);
        }
    }

    /// <summary>
    /// Whether the inputs and outputs have usable names, and the Function was read to its <c>end</c>. An output its
    /// body never defines is reported here too, but doesn't stop calls.
    /// </summary>
    private bool CheckHeader(FunctionDefinition function)
    {
        var nameErrors = GetFirstLineNameErrors(function.Inputs, function.Outputs, Functions.ContainsKey);
        foreach (var (parameter, message) in nameErrors)
        {
            Error(parameter, message);
        }

        if (function.IsComplete)
        {
            foreach (var output in function.Outputs.Where(o => nameErrors.All(e => e.Parameter != o)))
            {
                if (!function.Body.OfType<Definition>().Any(d => d.Name == output.Name))
                    Error(output, $"'{function.Name.Name}' never defines its output '{output.Name}'");
            }
        }

        return function.IsComplete && nameErrors.Count == 0;
    }

    /// <summary>
    /// Why each input and output that can't have its name can't: it's reserved or a Function's, written twice, or an
    /// output with an input's name. The editor checks a first line with these too.
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="outputs"></param>
    /// <param name="isFunction">Whether the scope has a Function by the name.</param>
    internal static List<(FunctionParameter Parameter, string Message)> GetFirstLineNameErrors(
        List<FunctionParameter> inputs,
        List<FunctionParameter> outputs,
        Func<string, bool> isFunction)
    {
        List<(FunctionParameter, string)> errors = [];
        HashSet<string> inputNames = [];
        foreach (var input in inputs)
        {
            var message = GetNameError(input.Name, isFunction)
                ?? (inputNames.Add(input.Name) ? null : $"'{input.Name}' is already an input");
            if (message != null)
                errors.Add((input, message));
        }

        HashSet<string> outputNames = [];
        foreach (var output in outputs)
        {
            var message = GetNameError(output.Name, isFunction)
                ?? (inputNames.Contains(output.Name) ? $"'{output.Name}' is an input. Give the output a name of its own" : null)
                ?? (outputNames.Add(output.Name) ? null : $"'{output.Name}' is already an output");
            if (message != null)
                errors.Add((output, message));
        }

        return errors;
    }

    /// <summary>
    /// A Function's own name is checked before every Function is known, so another Function's name doesn't count.
    /// </summary>
    private bool CheckName(FunctionParameter parameter)
    {
        var message = GetReservedNameError(parameter.Name);
        if (message != null)
            Error(parameter, message);

        return message == null;
    }

    /// <summary>
    /// Checks the body of each Function no call expanded, so its errors show while it's being written. Its inputs
    /// stand for nothing, like failed definitions, and what it placed is taken out again.
    /// </summary>
    private void CheckUncalledFunctions()
    {
        foreach (var function in _scriptFunctions.Values.ToList())
        {
            if (_brokenFunctions.Contains(function) || _expandedFunctions.Contains(function))
                continue;

            var snapshot = new Snapshot(this);
            Expand(function, null, scope => scope.FailedNames.UnionWith(function.Inputs.Select(i => i.Name)));
            ConnectDeferredWires(snapshot.DeferredWireCount);
            snapshot.Restore();
        }
    }
}
