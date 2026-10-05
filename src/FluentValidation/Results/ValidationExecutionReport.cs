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
/// The outcome of a validation run together with its explicit rule execution report.
/// </summary>
public sealed class ValidationExecutionReport {
	/// <summary>
	/// Creates a new validation execution report.
	/// </summary>
	/// <param name="result">The validation result.</param>
	/// <param name="records">The rule execution records, in the order the rules were processed.</param>
	public ValidationExecutionReport(ValidationResult result, IReadOnlyList<RuleExecutionRecord> records) {
		Result = result;
		Records = records;
	}

	/// <summary>
	/// The validation result. This is identical to the result that a regular call to
	/// Validate/ValidateAsync produces for the same instance - enabling the report does
	/// not change validation behaviour or invoke validators any additional times.
	/// </summary>
	public ValidationResult Result { get; }

	/// <summary>
	/// The rule execution records, in the order the rules were processed.
	/// Rules that never ran are recorded with <see cref="RuleExecutionCategory.NotExecuted"/>
	/// (or the applicable skip category) rather than being treated as passed.
	/// </summary>
	public IReadOnlyList<RuleExecutionRecord> Records { get; }
}
