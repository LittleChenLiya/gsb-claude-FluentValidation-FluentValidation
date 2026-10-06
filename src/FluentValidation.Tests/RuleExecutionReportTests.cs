#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

namespace FluentValidation.Tests;

using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using Xunit;

public class RuleExecutionReportTests {
	[Fact]
	public async Task Dependent_rule_is_never_invoked_and_marked_not_executed_when_parent_fails() {
		var validator = new TestValidator();
		int childInvocations = 0;
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).Must(_ => {
					childInvocations++;
					return true;
				});
			});

		var context = new ValidationContext<Person>(new Person {Surname = null});
		var result = await validator.ValidateAsync(context);

		// The dependent rule validator was never invoked.
		childInvocations.ShouldEqual(0);
		// Only the parent rule produced a failure.
		result.Errors.Count.ShouldEqual(1);
		result.Errors.Single().PropertyName.ShouldEqual("Surname");

		// The dependent rule is explicitly reported as not executed, by its own path.
		var record = context.ExecutionReport.Records.Single(r => r.Path == "Forename");
		record.Category.ShouldEqual(RuleExecutionCategory.NotExecuted);
		// A rule that did not run must never be reported as passed.
		context.ExecutionReport.Records.Any(r => r.Path == "Forename" && r.Category == RuleExecutionCategory.Succeeded).ShouldBeFalse();
		context.ExecutionReport.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.Failed);
	}

	[Fact]
	public async Task Nested_dependent_rules_are_all_marked_not_executed_when_parent_fails() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).NotNull()
					.DependentRules(() => {
						validator.RuleFor(x => x.Address).NotNull();
					});
			});

		var (_, report) = await validator.ValidateWithExecutionReportAsync(new Person {Surname = null});

		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.NotExecuted);
		report.Records.Single(r => r.Path == "Address").Category.ShouldEqual(RuleExecutionCategory.NotExecuted);
	}

	[Fact]
	public async Task Dependent_rule_executes_and_is_reported_when_parent_succeeds() {
		var validator = new TestValidator();
		int childInvocations = 0;
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).Must(f => {
					childInvocations++;
					return f != null;
				});
			});

		var (result, report) = await validator.ValidateWithExecutionReportAsync(new Person {Surname = "foo"});

		childInvocations.ShouldEqual(1);
		result.Errors.Single().PropertyName.ShouldEqual("Forename");
		report.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.Succeeded);
		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.Failed);
	}

	[Fact]
	public async Task Async_condition_false_is_categorised_separately_from_validation_failure() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull().WhenAsync((person, ct) => Task.FromResult(false));
		validator.RuleFor(x => x.Forename).NotNull();

		var (result, report) = await validator.ValidateWithExecutionReportAsync(new Person());

		// The async When condition evaluated to false: not a validation failure.
		report.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.ConditionNotMet);
		// A genuine validation failure is categorised differently.
		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.Failed);
		result.Errors.Count.ShouldEqual(1);
		result.Errors.Single().PropertyName.ShouldEqual("Forename");
	}

	[Fact]
	public void Cascade_stop_is_attributed_to_the_rule_that_sourced_the_stop() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).Cascade(CascadeMode.Stop).NotNull().NotEmpty();
		validator.RuleFor(x => x.Forename).NotNull();

		var (result, report) = validator.ValidateWithExecutionReport(new Person {Surname = null, Forename = "foo"});

		// The rule that triggered the stop is preserved as the source of the stop.
		report.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.CascadeStopped);
		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.Succeeded);
		// Only the first validator in the chain ran.
		result.Errors.Count.ShouldEqual(1);
		result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}

	[Fact]
	public async Task Concurrent_validation_calls_have_isolated_reports() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).MustAsync(async (forename, ct) => {
					await Task.Delay(20, ct);
					return forename != null;
				});
			});

		var failingContext = new ValidationContext<Person>(new Person {Surname = null});
		var passingContext = new ValidationContext<Person>(new Person {Surname = "foo", Forename = "bar"});

		var failingTask = validator.ValidateAsync(failingContext);
		var passingTask = validator.ValidateAsync(passingContext);
		await Task.WhenAll(failingTask, passingTask);

		// Each context carries its own report; nothing leaks across concurrent calls.
		failingContext.ExecutionReport.Records.Count.ShouldEqual(2);
		failingContext.ExecutionReport.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.NotExecuted);

		passingContext.ExecutionReport.Records.Count.ShouldEqual(2);
		passingContext.ExecutionReport.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.Succeeded);
		passingContext.ExecutionReport.Records.Any(r => r.Category == RuleExecutionCategory.NotExecuted).ShouldBeFalse();

		failingTask.Result.Errors.Single().PropertyName.ShouldEqual("Surname");
		passingTask.Result.IsValid.ShouldBeTrue();
	}

	[Fact]
	public async Task Report_does_not_change_results_or_validator_invocation_counts() {
		int invocations = 0;
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).Must(s => {
				invocations++;
				return s != null;
			})
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).Must(f => {
					invocations++;
					return f != null;
				});
			});

		var person = new Person {Surname = "foo", Forename = null};

		var normal = await validator.ValidateAsync(person);
		invocations.ShouldEqual(2);

		invocations = 0;
		var (withReport, report) = await validator.ValidateWithExecutionReportAsync(person);

		// Same number of validator invocations, same failures.
		invocations.ShouldEqual(2);
		withReport.Errors.Select(e => e.PropertyName).SequenceEqual(normal.Errors.Select(e => e.PropertyName)).ShouldBeTrue();
		withReport.Errors.Select(e => e.ErrorMessage).SequenceEqual(normal.Errors.Select(e => e.ErrorMessage)).ShouldBeTrue();
		report.Records.Count.ShouldEqual(2);
	}

	[Fact]
	public async Task Three_rule_example_reports_each_rule_by_its_own_identity() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).NotNull();
				validator.RuleFor(x => x.Address).NotNull();
			});

		// Parent succeeds, one dependent fails, one succeeds.
		var (result, report) = await validator.ValidateWithExecutionReportAsync(new Person {Surname = "foo", Forename = "bar"});

		report.Records.Count.ShouldEqual(3);
		report.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.Succeeded);
		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.Succeeded);
		report.Records.Single(r => r.Path == "Address").Category.ShouldEqual(RuleExecutionCategory.Failed);
		result.Errors.Single().PropertyName.ShouldEqual("Address");

		// Parent fails: both dependent rules are reported as not executed.
		(result, report) = await validator.ValidateWithExecutionReportAsync(new Person {Surname = null});

		report.Records.Count.ShouldEqual(3);
		report.Records.Single(r => r.Path == "Surname").Category.ShouldEqual(RuleExecutionCategory.Failed);
		report.Records.Single(r => r.Path == "Forename").Category.ShouldEqual(RuleExecutionCategory.NotExecuted);
		report.Records.Single(r => r.Path == "Address").Category.ShouldEqual(RuleExecutionCategory.NotExecuted);
		result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}

	[Fact]
	public async Task Reports_are_not_carried_over_between_calls() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull();

		var first = await validator.ValidateWithExecutionReportAsync(new Person {Surname = "foo"});
		var second = await validator.ValidateWithExecutionReportAsync(new Person {Surname = "foo"});

		first.Report.Records.Count.ShouldEqual(1);
		second.Report.Records.Count.ShouldEqual(1);
		(first.Report == second.Report).ShouldBeFalse();
	}
}
