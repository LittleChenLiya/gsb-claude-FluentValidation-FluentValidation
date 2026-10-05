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

/// <summary>
/// Categorizes the outcome of a single rule within a validation run.
/// </summary>
public enum RuleExecutionCategory {
	/// <summary>
	/// The rule was executed and all of its validators passed.
	/// </summary>
	Succeeded,

	/// <summary>
	/// The rule was executed and at least one of its validators failed.
	/// </summary>
	Failed,

	/// <summary>
	/// The rule was not executed because its synchronous When/Unless condition was not met.
	/// </summary>
	SkippedByCondition,

	/// <summary>
	/// The rule was not executed because its asynchronous When/Unless condition was not met.
	/// This is attributed separately from a validation failure: the rule's validators never ran.
	/// </summary>
	SkippedByAsyncCondition,

	/// <summary>
	/// Execution of the rule (or of the remaining rules in the validator) was stopped by
	/// <see cref="CascadeMode.Stop"/> after a failure. The stop source is preserved.
	/// </summary>
	StoppedByCascade,

	/// <summary>
	/// The rule is a dependent rule that was not executed because its parent rule did not
	/// succeed. Dependent rules are only entered when the parent rule actually succeeds.
	/// </summary>
	NotExecuted,
}
