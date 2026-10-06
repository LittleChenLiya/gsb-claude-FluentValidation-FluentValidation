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

## Execution reports

Because dependent rules only run when their parent rule succeeds, it can be hard to tell afterwards which rules actually executed. Every validation run produces an explicit execution report, exposed as `ExecutionReport` on the `ValidationContext<T>` (or returned by the `ValidateWithExecutionReport`/`ValidateWithExecutionReportAsync` extension methods). The report is created fresh for each `Validate`/`ValidateAsync` call, is never shared between calls (including concurrent ones), does not change the `ValidationResult`, and does not cause any additional validator invocations.

Each record in the report contains only the rule's path and a category:

| Category | Meaning |
|----------|---------|
| `Succeeded` | The rule executed and produced no failures. |
| `Failed` | The rule executed and produced at least one validation failure. |
| `ConditionNotMet` | The rule was skipped because a `When`/`Unless` condition (synchronous or asynchronous) evaluated to false. This is reported separately from a validation failure. |
| `CascadeStopped` | The rule failed and its `CascadeMode.Stop` behaviour halted the remaining validators in the rule. The record identifies the rule that was the source of the stop. |
| `NotExecuted` | A dependent rule that was never entered because its parent rule did not succeed. Rules that did not run are never reported as `Succeeded`. |

### Three-rule example

```csharp
var validator = new InlineValidator<Person>();
validator.RuleFor(x => x.Surname).NotNull()
  .DependentRules(() => {
    validator.RuleFor(x => x.Forename).NotNull();
    validator.RuleFor(x => x.Address).NotNull();
  });

var (result, report) = await validator.ValidateWithExecutionReportAsync(person);

foreach (var record in report.Records) {
  Console.WriteLine($"{record.Path}: {record.Category}");
}
```

If `Surname` is null, the parent rule fails and the two dependent rules never execute. The report contains three records: `Surname: Failed`, `Forename: NotExecuted` and `Address: NotExecuted`. If `Surname` is set, the dependent rules execute and are reported individually as `Succeeded` or `Failed` by their own paths.

If you already work with a custom `ValidationContext<T>`, the same report is available without the extension methods:

```csharp
var context = new ValidationContext<Person>(person);
var result = await validator.ValidateAsync(context);
var report = context.ExecutionReport;
```

### Running the tests locally

The execution report behaviour is covered by `RuleExecutionReportTests`. To run just these tests with a locally installed .NET SDK:

```bash
dotnet test src/FluentValidation.Tests/FluentValidation.Tests.csproj --framework net10.0 --filter "FullyQualifiedName~RuleExecutionReportTests"
```

Or to build only the main library:

```bash
dotnet build src/FluentValidation/FluentValidation.csproj
```