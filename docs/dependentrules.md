# Dependent Rules


By default, all rules in FluentValidation are separate and cannot influence one another. This is intentional and necessary for asynchronous validation to work. However, there may be some cases where you want to ensure that some rules are only executed after another has completed. You can use `DependentRules` to do this.

To use dependent rules, call the `DependentRules` method at the end of the rule that you want others to depend on. This method accepts a lambda expression inside which you can define other rules that will be executed only if the first rule passes:

```csharp
RuleFor(x => x.Surname).NotNull().DependentRules(() => {
  RuleFor(x => x.Forename).NotNull();
});
```

Here the rule against Forename will only be run if the Surname rule passes.

_Author's note_: Personally I do not particularly like using dependent rules as I feel it's fairly hard to read, especially with a complex set of rules. In many cases, it can be simpler to use `When` conditions combined with `CascadeMode` to prevent rules from running in certain situations. Even though this can sometimes mean more duplication, it is often easier to read.

## Execution reports for dependent rules

If you need to know exactly which rules ran and why others did not, you can use `ValidateWithExecutionReport` / `ValidateWithExecutionReportAsync`. These perform a regular validation (the `Result` is identical to what `Validate`/`ValidateAsync` returns for the same instance, and no validator is invoked any additional times) and additionally produce a list of `RuleExecutionRecord` entries. Each record contains only the rule's own `Path` and a `RuleExecutionCategory`:

| Category | Meaning |
|----------|---------|
| `Succeeded` | The rule executed and all of its validators passed. |
| `Failed` | The rule executed and at least one validator failed. |
| `SkippedByCondition` | The rule did not run because its synchronous `When`/`Unless` condition was not met. |
| `SkippedByAsyncCondition` | The rule did not run because its asynchronous `WhenAsync`/`UnlessAsync` condition was not met (attributed separately from a validation failure). |
| `StoppedByCascade` | Execution was stopped by `CascadeMode.Stop` after a failure (the stop source is preserved). |
| `NotExecuted` | A dependent rule that was never entered because its parent rule did not succeed. |

Dependent rules are only entered when the parent rule actually succeeds. A dependent rule that never ran is recorded as `NotExecuted` - it is never reported as passed. Each call uses a fresh context, so reports are not chained across calls and concurrent validation runs are isolated from one another.

Example with three rules (one parent rule with two dependent rules):

```csharp
var validator = new InlineValidator<Person>();
validator.RuleFor(x => x.Surname).NotNull().DependentRules(() => {
  validator.RuleFor(x => x.Forename).NotNull();
  validator.RuleFor(x => x.Address).NotNull();
});

var report = validator.ValidateWithExecutionReport(new Person { Surname = null });

// report.Result.Errors contains only the Surname failure.
// report.Records:
//   Surname: Failed
//   Forename: NotExecuted
//   Address: NotExecuted
```

Because the Surname rule failed, the two dependent rules were never entered, and the report marks them `NotExecuted` rather than treating them as passed.

The focused tests for this feature live in `src/FluentValidation.Tests/RuleExecutionReportTests.cs`. To run them locally from the repository root (requires a .NET 10 SDK):

```
dotnet test src/FluentValidation.Tests/FluentValidation.Tests.csproj -f net10.0 --filter "FullyQualifiedName~RuleExecutionReportTests"
```
