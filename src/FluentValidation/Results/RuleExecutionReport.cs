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

namespace FluentValidation.Results;

using System.Collections.Generic;

/// <summary>
/// Categorizes the outcome of a single rule during a validation run.
/// </summary>
public enum RuleExecutionCategory {
	/// <summary>
	/// The rule was executed and produced no validation failures.
	/// </summary>
	Succeeded,

	/// <summary>
	/// The rule was executed and produced at least one validation failure.
	/// </summary>
	Failed,

	/// <summary>
	/// The rule was not executed because its When/Unless condition (synchronous or
	/// asynchronous) evaluated to false. This is distinct from a validation failure.
	/// </summary>
	ConditionNotMet,

	/// <summary>
	/// The rule produced a failure and its <see cref="CascadeMode.Stop"/> behaviour halted
	/// execution of the remaining validators in the rule. The record identifies the rule
	/// that was the source of the stop.
	/// </summary>
	CascadeStopped,

	/// <summary>
	/// The rule was never executed. This happens for dependent rules whose parent rule
	/// did not complete successfully (the parent failed or its condition was not met).
	/// Rules that were not executed are never reported as <see cref="Succeeded"/>.
	/// </summary>
	NotExecuted,
}

/// <summary>
/// A single entry in a <see cref="RuleExecutionReport"/>. A record contains only the
/// path of the rule and the category of its execution outcome.
/// </summary>
public sealed class RuleExecutionRecord {
	/// <summary>
	/// Creates a new execution record.
	/// </summary>
	/// <param name="path">The full property path of the rule.</param>
	/// <param name="category">The execution outcome category.</param>
	public RuleExecutionRecord(string path, RuleExecutionCategory category) {
		Path = path;
		Category = category;
	}

	/// <summary>
	/// The full property path identifying the rule that was (or was not) executed.
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// The category of the execution outcome.
	/// </summary>
	public RuleExecutionCategory Category { get; }

	/// <inheritdoc />
	public override string ToString() => $"{Path}: {Category}";
}

/// <summary>
/// An explicit report of which rules were executed during a single validation run and
/// how each run concluded. A new report is created for every validation context, so
/// reports are never shared or carried over between separate Validate/ValidateAsync calls.
/// </summary>
public sealed class RuleExecutionReport {
	private readonly List<RuleExecutionRecord> _records = new();

	/// <summary>
	/// The execution records in the order they were produced.
	/// Each record contains only the rule path and its execution category.
	/// </summary>
	public IReadOnlyList<RuleExecutionRecord> Records => _records;

	internal void Add(string path, RuleExecutionCategory category)
		=> _records.Add(new RuleExecutionRecord(path, category));
}
