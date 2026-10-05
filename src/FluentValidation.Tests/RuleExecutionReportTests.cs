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
	public void Parent_failure_marks_dependent_rule_not_executed_and_does_not_invoke_it() {
		var validator = new TestValidator();
		int childInvocations = 0;
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).Must(_ => {
					childInvocations++;
					return true;
				});
			});

		var report = validator.ValidateWithExecutionReport(new Person { Surname = null });

		// The dependent rule validator was never invoked.
		childInvocations.ShouldEqual(0);

		// Reported by actual rule identity, and not recorded as passed.
		report.Records.Count.ShouldEqual(2);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.NotExecuted);
		report.Records.Any(r => r.Path == "Forename" && r.Category == RuleExecutionCategory.Succeeded).ShouldBeFalse();

		// The result is identical to a normal validation run.
		var normal = validator.Validate(new Person { Surname = null });
		Assert.Equal(normal.Errors.Select(e => e.PropertyName), report.Result.Errors.Select(e => e.PropertyName));
		report.Result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}

	[Fact]
	public void Parent_success_enters_dependent_rules_and_reports_each_rule_by_its_own_path() {
		// Three-rule example: one parent rule with two dependent rules.
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).NotNull();
				validator.RuleFor(x => x.Address).NotNull();
			});

		var report = validator.ValidateWithExecutionReport(new Person { Surname = "a", Forename = "b", Address = new Address() });

		report.Result.IsValid.ShouldBeTrue();
		report.Records.Count.ShouldEqual(3);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Succeeded);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.Succeeded);
		AssertRecord(report.Records[2], "Address", RuleExecutionCategory.Succeeded);

		// Parent fails -> both dependent rules are marked not executed and produce no errors.
		report = validator.ValidateWithExecutionReport(new Person { Surname = null });
		report.Records.Count.ShouldEqual(3);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.NotExecuted);
		AssertRecord(report.Records[2], "Address", RuleExecutionCategory.NotExecuted);
		report.Result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}

	[Fact]
	public void Nested_dependent_rules_are_all_marked_not_executed_when_parent_fails() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).NotNull()
					.DependentRules(() => {
						validator.RuleFor(x => x.Address).NotNull();
					});
			});

		var report = validator.ValidateWithExecutionReport(new Person { Surname = null });

		report.Records.Count.ShouldEqual(3);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.NotExecuted);
		AssertRecord(report.Records[2], "Address", RuleExecutionCategory.NotExecuted);
	}
	[Fact]
	public async Task Async_condition_false_is_categorized_differently_from_validation_failure() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull();
		validator.RuleFor(x => x.Forename).NotNull().WhenAsync((p, ct) => Task.FromResult(false));

		var report = await validator.ValidateWithExecutionReportAsync(new Person { Surname = null, Forename = null });

		report.Records.Count.ShouldEqual(2);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		// The async When evaluated to false: the rule never ran, which is not a validation failure.
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.SkippedByAsyncCondition);
		(report.Records[0].Category != report.Records[1].Category).ShouldBeTrue();

		// Same result as a regular ValidateAsync call.
		var normal = await validator.ValidateAsync(new Person { Surname = null, Forename = null });
		Assert.Equal(normal.Errors.Select(e => e.PropertyName), report.Result.Errors.Select(e => e.PropertyName));
		report.Result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}

	[Fact]
	public void Sync_condition_false_is_categorized_as_skipped_not_failed() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull().When(p => p.Age > 100);
		validator.RuleFor(x => x.Forename).NotNull();

		var report = validator.ValidateWithExecutionReport(new Person { Surname = null, Forename = null });

		report.Records.Count.ShouldEqual(2);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.SkippedByCondition);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.Failed);
		report.Result.Errors.Single().PropertyName.ShouldEqual("Forename");
	}

	[Fact]
	public void Rule_level_cascade_stop_preserves_the_stop_source() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).Cascade(CascadeMode.Stop).NotNull().NotEqual("foo");
		validator.RuleFor(x => x.Forename).NotNull();

		var report = validator.ValidateWithExecutionReport(new Person { Surname = null, Forename = null });

		report.Records.Count.ShouldEqual(2);
		// NotNull failed and the cascade stopped NotEqual from running: the stop source is preserved.
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.StoppedByCascade);
		// No cascade on this rule: a plain failure.
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.Failed);
		report.Result.Errors.Count.ShouldEqual(2);
	}

	[Fact]
	public void Class_level_cascade_stop_preserves_the_stop_source() {
		var validator = new TestValidator();
		validator.ClassLevelCascadeMode = CascadeMode.Stop;
		validator.RuleFor(x => x.Surname).NotNull();
		validator.RuleFor(x => x.Forename).NotNull();

		var report = validator.ValidateWithExecutionReport(new Person { Surname = null, Forename = null });

		report.Records.Count.ShouldEqual(2);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		// The second rule never ran because of the class-level cascade stop.
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.StoppedByCascade);
		report.Result.Errors.Single().PropertyName.ShouldEqual("Surname");
	}
	[Fact]
	public async Task Concurrent_validation_runs_have_isolated_reports() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname)
			.MustAsync(async (s, ct) => {
				await Task.Delay(20);
				return s != null;
			})
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename)
					.MustAsync(async (f, ct) => {
						await Task.Delay(20);
						return f != null;
					});
			});

		var failingTask = validator.ValidateWithExecutionReportAsync(new Person { Surname = null });
		var passingTask = validator.ValidateWithExecutionReportAsync(new Person { Surname = "a", Forename = "b" });
		await Task.WhenAll(failingTask, passingTask);

		var failing = failingTask.Result;
		failing.Records.Count.ShouldEqual(2);
		AssertRecord(failing.Records[0], "Surname", RuleExecutionCategory.Failed);
		AssertRecord(failing.Records[1], "Forename", RuleExecutionCategory.NotExecuted);

		var passing = passingTask.Result;
		passing.Records.Count.ShouldEqual(2);
		AssertRecord(passing.Records[0], "Surname", RuleExecutionCategory.Succeeded);
		AssertRecord(passing.Records[1], "Forename", RuleExecutionCategory.Succeeded);

		// No cross-contamination between the two concurrent contexts.
		passing.Records.Any(r => r.Category == RuleExecutionCategory.NotExecuted).ShouldBeFalse();
		failing.Records.Any(r => r.Category == RuleExecutionCategory.Succeeded).ShouldBeFalse();
	}

	[Fact]
	public async Task Reports_are_not_chained_across_calls() {
		var validator = new TestValidator();
		validator.RuleFor(x => x.Surname).NotNull()
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).NotNull();
			});

		var first = validator.ValidateWithExecutionReport(new Person { Surname = null });
		var second = validator.ValidateWithExecutionReport(new Person { Surname = "a", Forename = "b" });
		var third = await validator.ValidateWithExecutionReportAsync(new Person { Surname = "a", Forename = "b" });

		first.Records.Count.ShouldEqual(2);
		// Each call gets a fresh report: nothing leaks from the previous (failing) call.
		second.Records.Count.ShouldEqual(2);
		second.Records.All(r => r.Category == RuleExecutionCategory.Succeeded).ShouldBeTrue();
		third.Records.Count.ShouldEqual(2);
		third.Records.All(r => r.Category == RuleExecutionCategory.Succeeded).ShouldBeTrue();
	}
	[Fact]
	public async Task Report_does_not_invoke_validators_additional_times_and_matches_normal_result() {
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

		var person = new Person { Surname = "a", Forename = "b" };

		var report = validator.ValidateWithExecutionReport(person);
		invocations.ShouldEqual(2);

		var normal = validator.Validate(person);
		// One invocation per validator per call: the report added none.
		invocations.ShouldEqual(4);

		Assert.Equal(normal.Errors.Select(e => e.PropertyName), report.Result.Errors.Select(e => e.PropertyName));
		report.Result.IsValid.ShouldEqual(normal.IsValid);

		var asyncReport = await validator.ValidateWithExecutionReportAsync(person);
		invocations.ShouldEqual(6);
		var normalAsync = await validator.ValidateAsync(person);
		invocations.ShouldEqual(8);
		Assert.Equal(normalAsync.Errors.Select(e => e.PropertyName), asyncReport.Result.Errors.Select(e => e.PropertyName));
	}

	[Fact]
	public async Task Async_parent_failure_marks_dependent_rule_not_executed() {
		var validator = new TestValidator();
		int childInvocations = 0;
		validator.RuleFor(x => x.Surname)
			.MustAsync(async (s, ct) => {
				await Task.Delay(1);
				return s != null;
			})
			.DependentRules(() => {
				validator.RuleFor(x => x.Forename).Must(_ => {
					childInvocations++;
					return true;
				});
			});

		var report = await validator.ValidateWithExecutionReportAsync(new Person { Surname = null });

		childInvocations.ShouldEqual(0);
		report.Records.Count.ShouldEqual(2);
		AssertRecord(report.Records[0], "Surname", RuleExecutionCategory.Failed);
		AssertRecord(report.Records[1], "Forename", RuleExecutionCategory.NotExecuted);
	}

	private static void AssertRecord(RuleExecutionRecord record, string path, RuleExecutionCategory category) {
		record.Path.ShouldEqual(path);
		record.Category.ShouldEqual(category);
	}
}
